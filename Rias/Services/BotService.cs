using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Serilog;

namespace Rias.Services
{
    [AutoStart]
    public class BotService : RiasService
    {
        public readonly ConcurrentDictionary<ulong, List<DiscordWebhook>> Webhooks = new();
        public readonly ConcurrentDictionary<ulong, TaskCompletionSource> GuildsTcs = new();
        
        private readonly ConcurrentDictionary<int, bool> _shardsReady = new();
        private readonly ConcurrentHashSet<ulong> _unchunkedGuilds = new();

        private readonly HttpClient _discordBotsHttpClient;
        private readonly string[] _codeLanguages = { "cs", "csharp" };
        
        private Timer? _dblTimer;
        private Timer? _dbTimer;

        public BotService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            RiasBot.Client.Ready += ShardReadyAsync;
            RiasBot.Client.GuildMemberAdded += GuildMemberAddedAsync;
            RiasBot.Client.GuildMemberRemoved += GuildMemberRemovedAsync;
            RiasBot.Client.GuildMemberUpdated += GuildMemberUpdatedAsync;
            RiasBot.Client.GuildMembersChunked += GuildMembersChunkedAsync;
            RiasBot.Client.GuildDownloadCompleted += GuildDownloadCompletedAsync;
            
            RunTaskAsync(RequestMembersAsync);
            
            _discordBotsHttpClient = new HttpClient();
        }
        
        public static string ReplacePlaceholders(DiscordUser user, string message)
        {
            var username = user.FullName().Replace("\\", "\\\\").Replace("\"", "\\\"");
            var sb = new StringBuilder(message)
                .Replace("%member%", username)
                .Replace("%user%", username)
                .Replace("%avatar%", user.GetAvatarUrl(ImageFormat.Auto));

            if (user is DiscordMember member)
            {
                var guildName = member.Guild.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.Replace("%mention%", member.Mention)
                    .Replace("%guild%", guildName)
                    .Replace("%server%", guildName)
                    .Replace("%members%", member.Guild.MemberCount.ToString());
            }

            return sb.ToString();
        }
        
        public async Task AddAssignableRoleAsync(DiscordMember member)
        {
            var currentMember = member.Guild.CurrentMember;
            if (!currentMember.GetPermissions().HasPermission(Permissions.ManageRoles))
                return;

            var memberRoles = member.Roles.ToList();
            if (memberRoles.Count != 0)
                return;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id);
            if (guildDb is null)
                return;

            var aar = member.Guild.GetRole(guildDb.AutoAssignableRoleId);
            if (aar is null)
                return;

            if (currentMember.CheckRoleHierarchy(aar) > 0
                && !aar.IsManaged
                && memberRoles.All(x => x.Id != aar.Id))
                await member.GrantRoleAsync(aar);
        }
        
        public async Task<EvaluationDetails> EvaluateAsync(RiasCommandContext context, string code)
        {
            var globals = new RoslynGlobals
            {
                RiasBot = RiasBot,
                Context = context
            };

            var imports = new[]
            {
                "System", "System.Collections.Generic", "System.Linq", "DSharpPlus", "DSharpPlus.Interactivity",
                "System.Threading.Tasks", "System.Text", "Microsoft.Extensions.DependencyInjection", "System.Net.Http",
                "Rias", "Rias.Extensions", "Rias.Database", "Qmmands"
            };
            
            var scriptOptions = ScriptOptions.Default.WithReferences(typeof(RiasBot).Assembly).AddImports(imports);
            code = SanitizeCode(code);
            
            using var loader = new InteractiveAssemblyLoader();
            var sw = Stopwatch.StartNew();
            var script = CSharpScript.Create(code, scriptOptions, typeof(RoslynGlobals), loader);
            var diagnostics = script.Compile();
            sw.Stop();
            
            var compilationTime = sw.Elapsed;
            
            if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
            {
                return new EvaluationDetails
                {
                    CompilationTime = compilationTime,
                    Code = code,
                    IsCompiled = false,
                    Exception = string.Join("\n", diagnostics.Select(x => x.ToString()))
                };
            }
            
            sw.Restart();

            try
            {
                var result = await script.RunAsync(globals);
                sw.Stop();

                var evaluationDetails = new EvaluationDetails
                {
                    CompilationTime = compilationTime,
                    ExecutionTime = sw.Elapsed,
                    Code = code,
                    IsCompiled = true,
                    Success = true
                };

                var returnValue = result.ReturnValue;
                var type = result.ReturnValue?.GetType();

                switch (returnValue)
                {
                    case string str:
                        evaluationDetails.Result = string.Equals(str, string.Empty) ? "empty" : str;
                        evaluationDetails.ReturnType = type?.Name;
                        break;

                    case IEnumerable enumerable:
                        var list = enumerable.Cast<object>().ToList();
                        var enumType = enumerable.GetType();

                        evaluationDetails.Result = list.Count != 0 ? $"[{string.Join(", ", list)}]" : "empty";
                        evaluationDetails.ReturnType = $"{enumType.Name}<{string.Join(", ", enumType.GenericTypeArguments.Select(t => t.Name))}>";
                        break;

                    case Enum @enum:
                        evaluationDetails.Result = @enum.ToString();
                        evaluationDetails.ReturnType = @enum.GetType().Name;
                        break;

                    default:
                        evaluationDetails.Result = returnValue?.ToString();
                        evaluationDetails.ReturnType = type?.Name;
                        break;
                }

                return evaluationDetails;
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new EvaluationDetails
                {
                    CompilationTime = compilationTime,
                    ExecutionTime = sw.Elapsed,
                    Code = code,
                    ReturnType = ex.GetType().Name,
                    IsCompiled = true,
                    Success = false,
                    Exception = ex.Message
                };
            }
        }
        
        private async Task GuildMemberAddedAsync(DiscordClient client, GuildMemberAddEventArgs args)
        {
            if (RiasBot.CurrentUser != null && args.Member.Id == RiasBot.CurrentUser.Id)
                return;

            RiasBot.Members[args.Member.Id] = args.Member;
            
            await RunTaskAsync(SendGreetMessageAsync(args.Member));

            if (args.Member.IsPending is true)
                return;
            
            await RunTaskAsync(AddMuteRoleAsync(args.Member));
            await RunTaskAsync(AddXpIgnoredRole(args.Member));
            await RunTaskAsync(AddAssignableRoleAsync(args.Member));
            await RunTaskAsync(AddLevelRolesAsync(args.Member));
        }

        private async Task AddLevelRolesAsync(DiscordMember member)
        {
            var currentMember = member.Guild.CurrentMember;
            if (!currentMember.GetPermissions().HasPermission(Permissions.ManageRoles))
                return;
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var memberDb = await db.Members.FirstOrDefaultAsync(m => m.GuildId == member.Guild.Id && m.MemberId == member.Id);
            if (memberDb is null)
                return;
            
            var level = RiasUtilities.XpToLevel(memberDb.Xp, XpService.XpThreshold);
            var levelRoles = await db.GetListAsync<GuildXpRoleEntity>(lr => lr.GuildId == member.Guild.Id && lr.Level <= level);
            if (levelRoles.Count == 0)
                return;

            var roles = levelRoles.Select(lr => member.Guild.GetRole(lr.RoleId)).Where(r => r is not null);
            foreach (var role in roles)
            {
                if (currentMember.CheckRoleHierarchy(role) <= 0)
                    continue;
                
                if (member.Roles.All(x => x.Id != role.Id))
                    await member.GrantRoleAsync(role);
            }
        }
        
        private async Task SendGreetMessageAsync(DiscordMember member)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == member.Guild.Id);
            
            var guild = member.Guild;
            var currentMember = guild.CurrentMember;
            if (!currentMember.GetPermissions().HasPermission(Permissions.ManageWebhooks))
            {
                await DisableGreetAsync(guild);
                return;
            }
            
            if (guildDb is null) return;
            if (!guildDb.GreetNotification) return;
            if (string.IsNullOrEmpty(guildDb.GreetMessage)) return;

            if (!Webhooks.TryGetValue(guild.Id, out var webhooks))
            {
                webhooks = new List<DiscordWebhook>();
                Webhooks.TryAdd(guild.Id, webhooks);
            }

            var webhook = webhooks.FirstOrDefault(x => x.Id == guildDb.GreetWebhookId);
            if (webhook is null)
            {
                webhook = await guild.GetWebhookAsync(guildDb.GreetWebhookId);
                if (webhook is null)
                {
                    await DisableGreetAsync(guild);
                    return;
                }
                
                webhooks.Add(webhook);
            }

            var greetMsg = ReplacePlaceholders(member, guildDb.GreetMessage);
            if (RiasUtilities.TryParseMessage(greetMsg, out var customMessage))
            {
                await webhook.ExecuteAsync(new DiscordWebhookBuilder
                {
                    Username = currentMember.Username,
                    AvatarUrl = currentMember.AvatarUrl,
                    Content = customMessage.Content
                }.AddEmbed(customMessage.Embed).AddMention(new UserMention(member)));
            }
            else
            {
                await webhook.ExecuteAsync(new DiscordWebhookBuilder
                {
                    Username = currentMember.Username,
                    AvatarUrl = currentMember.AvatarUrl,
                    Content = greetMsg
                }.AddMention(new UserMention(member)));
            }
        }
        
        private async Task DisableGreetAsync(DiscordGuild guild)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            if (guildDb is null)
                return;
            
            guildDb.GreetNotification = false;
            await db.SaveChangesAsync();
        }

        private async Task AddMuteRoleAsync(DiscordMember member)
        {
            var currentUser = member.Guild.CurrentMember;
            if (!currentUser.GetPermissions().HasPermission(Permissions.ManageRoles))
                return;
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id);
            var memberDb = await db.Members.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id && x.MemberId == member.Id);
            
            if (memberDb is null)
                return;
            
            if (!memberDb.IsMuted)
                return;

            var role = member.Guild.GetRole(guildDb?.MuteRoleId ?? 0);
            if (role != null)
            {
                await member.GrantRoleAsync(role);
            }
            else
            {
                memberDb.IsMuted = false;
                await db.SaveChangesAsync();
            }
        }

        private async Task AddXpIgnoredRole(DiscordMember member)
        {
            var currentUser = member.Guild.CurrentMember;
            if (!currentUser.GetPermissions().HasPermission(Permissions.ManageRoles))
                return;
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id);
            var memberDb = await db.Members.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id && x.MemberId == member.Id);
            
            if (memberDb is null)
                return;
            
            if (!memberDb.IsXpIgnored)
                return;

            var role = member.Guild.GetRole(guildDb?.XpIgnoredRoleId ?? 0);
            if (role != null)
            {
                await member.GrantRoleAsync(role);
            }
            else
            {
                memberDb.IsXpIgnored = false;
                await db.SaveChangesAsync();
            }
        }
        
        private async Task GuildMemberRemovedAsync(DiscordClient client, GuildMemberRemoveEventArgs args)
        {
            if (RiasBot.CurrentUser != null && args.Member.Id == RiasBot.CurrentUser.Id)
                return;
            
            await RunTaskAsync(SendByeMessageAsync(args.Member));
        }

        private async Task SendByeMessageAsync(DiscordMember member)
        {
            var guild = member.Guild;
            var currentMember = guild.CurrentMember;
            if (!currentMember.GetPermissions().HasPermission(Permissions.ManageWebhooks))
            {
                await DisableByeAsync(guild);
                return;
            }
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            
            var guildDb = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == guild.Id);
            if (guildDb is null)
                return;
            
            if (!guildDb.ByeNotification)
                return;
            
            if (string.IsNullOrEmpty(guildDb.ByeMessage))
                return;
            
            if (!Webhooks.TryGetValue(guild.Id, out var webhooks))
            {
                webhooks = new List<DiscordWebhook>();
                Webhooks.TryAdd(guild.Id, webhooks);
            }
            
            var webhook = webhooks.FirstOrDefault(x => x.Id == guildDb.ByeWebhookId);
            if (webhook is null)
            {
                webhook = await guild.GetWebhookAsync(guildDb.ByeWebhookId);
                if (webhook is null)
                {
                    await DisableByeAsync(guild);
                    return;
                }
                
                webhooks.Add(webhook);
            }

            var byeMsg = ReplacePlaceholders(member, guildDb.ByeMessage);
            if (RiasUtilities.TryParseMessage(byeMsg, out var customMessage))
            {
                await webhook.ExecuteAsync(new DiscordWebhookBuilder
                {
                    Username = currentMember.Username,
                    AvatarUrl = currentMember.AvatarUrl,
                    Content = customMessage.Content
                }.AddEmbed(customMessage.Embed).AddMention(new UserMention(member)));
            }
            else
            {
                await webhook.ExecuteAsync(new DiscordWebhookBuilder
                {
                    Username = currentMember.Username,
                    AvatarUrl = currentMember.AvatarUrl,
                    Content = byeMsg
                }.AddMention(new UserMention(member)));
            }
        }
        
        private async Task DisableByeAsync(DiscordGuild guild)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            if (guildDb is null)
                return;
            
            guildDb.ByeNotification = false;
            await db.SaveChangesAsync();
        }

        private async Task ShardReadyAsync(DiscordClient client, ReadyEventArgs args)
        {
            _shardsReady.AddOrUpdate(client.ShardId, true, (_, _) => true);
            if (_shardsReady.Count == RiasBot.Client.ShardClients.Count && _shardsReady.All(x => x.Value))
            {
                RiasBot.Client.Ready -= ShardReadyAsync;
                Log.Information("All shards are connected");
                
                await RiasBot.Client.UseInteractivityAsync(new InteractivityConfiguration());
                RiasBot.GetRequiredService<MuteService>();
                
                var reactionsService = RiasBot.GetRequiredService<ReactionsService>();
#if DEBUG
                reactionsService.AddWeebUserAgent($"{RiasBot.CurrentUser!.Username}/{RiasBot.Version} (development)");
#else
                reactionsService.AddWeebUserAgent($"{RiasBot.CurrentUser!.Username}/{RiasBot.Version}");
#endif
                
                if (!string.IsNullOrEmpty(Configuration.DiscordBotListToken))
                    _dblTimer = new Timer(_ => RunTaskAsync(PostDiscordBotListStatsAsync), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                
                if (!string.IsNullOrEmpty(Configuration.DiscordBotsToken))
                    _dbTimer = new Timer(_ => RunTaskAsync(PostDiscordBotsStatsAsync), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            }
        }

        private async Task GuildMemberUpdatedAsync(DiscordClient client, GuildMemberUpdateEventArgs args)
        {
            RiasBot.Members[args.Member.Id] = args.Member;

            if (args.PendingBefore is true && args.PendingAfter is false)
            {
                await RunTaskAsync(AddMuteRoleAsync(args.Member));
                await RunTaskAsync(AddXpIgnoredRole(args.Member));
                await RunTaskAsync(AddAssignableRoleAsync(args.Member));
                await RunTaskAsync(AddLevelRolesAsync(args.Member));
            }
            
            if (args.RolesBefore.Count == args.RolesAfter.Count)
                return;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == args.Guild.Id);
            if (guildDb is null)
                return;

            var memberDb = await db.Members.FirstOrDefaultAsync(x => x.GuildId == args.Guild.Id && x.MemberId == args.Member.Id);
            if (memberDb is null)
                return;

            memberDb.IsMuted = args.Member.Roles.FirstOrDefault(x => x.Id == guildDb.MuteRoleId) != null;
            memberDb.IsXpIgnored = args.Member.Roles.FirstOrDefault(x => x.Id == guildDb.XpIgnoredRoleId) != null;
            await db.SaveChangesAsync();
        }

        private Task GuildMembersChunkedAsync(DiscordClient client, GuildMembersChunkEventArgs args)
        {
            if (args.ChunkIndex + 1 == args.ChunkCount && GuildsTcs.TryGetValue(args.Guild.Id, out var tcs))
                tcs.TrySetResult();

            foreach (var member in args.Members)
                RiasBot.Members[member.Id] = member;

            return Task.CompletedTask;
        }

        private Task GuildDownloadCompletedAsync(DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            foreach (var (guildId, _) in args.Guilds)
            {
                if (RiasBot.ChunkedGuilds.Contains(guildId))
                    _unchunkedGuilds.Add(guildId);
            }

            return Task.CompletedTask;
        }

        private async Task RequestMembersAsync()
        {
            var ct = CancellationToken.None;
            while (!ct.IsCancellationRequested)
            {
                while (!_unchunkedGuilds.IsEmpty)
                {
                    var guildId = _unchunkedGuilds.First();
                    _unchunkedGuilds.TryRemove(guildId);

                    var guild = RiasBot.GetGuild(guildId);
                    if (guild is null)
                        continue;
                    
                    Log.Debug($"Requesting members for {guild}");
                    await guild.RequestMembersAsync();
                    await Task.Delay(1000);
                }
                
                await Task.Delay(10000);
            }
        }
        
        private async Task PostDiscordBotListStatsAsync()
        {
            if (RiasBot.CurrentUser is null)
                return;
            
            try
            {
                using var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string?, string?>("shard_count", RiasBot.Client.ShardClients.Count.ToString()),
                    new KeyValuePair<string?, string?>("server_count", RiasBot.Client.ShardClients.Sum(x => x.Value.Guilds.Count).ToString())
                });
                
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"https://top.gg/api/bots/{RiasBot.CurrentUser.Id}/stats"),
                    Content = content
                };
                request.Headers.Add("Authorization", Configuration.DiscordBotListToken);
                
                await _discordBotsHttpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Log.Warning(ex.ToString());
            }
        }
        
        private async Task PostDiscordBotsStatsAsync()
        {
            if (RiasBot.CurrentUser is null)
                return;
            
            try
            {
                using var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string?, string?>("shardCount", RiasBot.Client.ShardClients.Count.ToString()),
                    new KeyValuePair<string?, string?>("guildCount", RiasBot.Client.ShardClients.Sum(x => x.Value.Guilds.Count).ToString())
                });
                
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"https://discord.bots.gg/api/v1/bots/{RiasBot.CurrentUser.Id}/stats"),
                    Content = content
                };
                request.Headers.Add("Authorization", Configuration.DiscordBotsToken);
                
                await _discordBotsHttpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Log.Warning(ex.ToString());
            }
        }

        private string SanitizeCode(string code)
        {
            code = code.Trim('`');

            foreach (var language in _codeLanguages)
            {
                var index = code.IndexOf('\n');
                if (index == -1)
                    break;

                var substring = code[..code.IndexOf('\n')];
                if (!string.IsNullOrEmpty(substring) && string.Equals(substring, language, StringComparison.OrdinalIgnoreCase))
                {
                    return code[language.Length..];
                }
            }

            return code;
        }
    }
}