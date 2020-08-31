using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Serilog;

namespace Rias.Core.Services
{
    [AutoStart]
    public class BotService : RiasService
    {
        public BotService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RiasBot.Client.Ready += ShardReadyAsync;
            RiasBot.Client.GuildMemberAdded += GuildMemberAddedAsync;
            RiasBot.Client.GuildMemberRemoved += GuildMemberRemovedAsync;
            RiasBot.Client.GuildMemberUpdated += GuildMemberUpdatedAsync;

            RiasBot.Client.GuildDownloadCompleted += args =>
            {
                foreach (var (id, member) in args.Guilds.SelectMany(x => x.Value.Members))
                    RiasBot.Members[id] = member;
                return Task.CompletedTask;
            };
            
            RiasBot.Client.UserUpdated += args =>
            {
                RiasBot.Members[args.UserAfter.Id] = args.UserAfter;
                return Task.CompletedTask;
            };
        }

        public readonly ConcurrentDictionary<ulong, List<DiscordWebhook>> Webhooks = new ConcurrentDictionary<ulong, List<DiscordWebhook>>();
        private readonly ConcurrentDictionary<int, bool> _shardsReady = new ConcurrentDictionary<int, bool>();
        
        private async Task GuildMemberAddedAsync(GuildMemberAddEventArgs args)
        {
            var member = args.Member;
            if (RiasBot.CurrentUser != null && member.Id == RiasBot.CurrentUser.Id)
                return;

            RiasBot.Members[args.Member.Id] = args.Member;
            await RunTaskAsync(AddAssignableRoleAsync(member));

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == member.Guild.Id);
            await SendGreetMessageAsync(guildDb, member);

            var currentUser = member.Guild.CurrentMember;
            if (!currentUser.GetPermissions().HasPermission(Permissions.ManageRoles)) return;

            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id && x.UserId == member.Id);
            if (userGuildDb is null) return;
            if (!userGuildDb.IsMuted) return;

            var role = member.Guild.GetRole(guildDb?.MuteRoleId ?? 0)
                       ?? member.Guild.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, MuteService.MuteRole)).Value;
            
            if (role != null)
                await member.GrantRoleAsync(role);
            else
            {
                userGuildDb.IsMuted = false;
                await db.SaveChangesAsync();
            }
        }
        
        private async Task SendGreetMessageAsync(GuildsEntity? guildDb, DiscordMember member)
        {
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

            try
            {
                var greetMsg = ReplacePlaceholders(member, guildDb.GreetMessage);
                if (RiasUtilities.TryParseMessage(greetMsg, out var customMessage))
                    await webhook.ExecuteAsync(new DiscordWebhookBuilder().WithContent(customMessage.Content).AddEmbed(customMessage.Embed));
                else
                    await webhook.ExecuteAsync(new DiscordWebhookBuilder().WithContent(greetMsg));
            }
            catch
            {
                await DisableGreetAsync(guild);
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
        
        private async Task GuildMemberRemovedAsync(GuildMemberRemoveEventArgs args)
        {
            if (RiasBot.CurrentUser != null && args.Member.Id == RiasBot.CurrentUser.Id)
                return;
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == args.Guild.Id);
            await SendByeMessageAsync(guildDb, args.Member);
        }

        private async Task SendByeMessageAsync(GuildsEntity? guildDb, DiscordMember member)
        {
            var guild = member.Guild;
            var currentMember = guild.CurrentMember;
            if (!currentMember.GetPermissions().HasPermission(Permissions.ManageWebhooks))
            {
                await DisableByeAsync(guild);
                return;
            }
            
            if (guildDb is null) return;
            if (!guildDb.ByeNotification) return;
            if (string.IsNullOrEmpty(guildDb.ByeMessage)) return;
            
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

            try
            {
                var byeMsg = ReplacePlaceholders(member, guildDb.ByeMessage);
                if (RiasUtilities.TryParseMessage(byeMsg, out var customMessage))
                    await webhook.ExecuteAsync(new DiscordWebhookBuilder().WithContent(customMessage.Content).AddEmbed(customMessage.Embed));
                else
                    await webhook.ExecuteAsync(new DiscordWebhookBuilder().WithContent(byeMsg));
            }
            catch
            {
                await DisableByeAsync(guild);
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

        private Task ShardReadyAsync(ReadyEventArgs e)
        {
            _shardsReady.AddOrUpdate(e.Client.ShardId, true, (k, v) => true);
            if (_shardsReady.Count == RiasBot.Client.ShardClients.Count && _shardsReady.All(x => x.Value))
            {
                RiasBot.Client.Ready -= ShardReadyAsync;
                Log.Information("All shards are connected");

                RiasBot.GetRequiredService<MuteService>();
                
                var reactionsService =  RiasBot.GetRequiredService<ReactionsService>();
#if DEBUG
                reactionsService.AddWeebUserAgent($"{RiasBot.CurrentUser!.Username}/{RiasBot.Version} (development)");
#else
                reactionsService.AddWeebUserAgent($"{RiasBot.CurrentUser!.Username}/{RiasBot.Version}");
#endif
            }

            return Task.CompletedTask;
        }
        
        public async Task AddAssignableRoleAsync(DiscordMember member)
        {
            var currentMember = member.Guild.CurrentMember;
            if (!currentMember.GetPermissions().HasPermission(Permissions.ManageRoles))
                return;

            var memberRoles = member.Roles.ToList();
            if (memberRoles.Count > 1) return;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id);
            if (guildDb is null)
                return;

            var aar = member.Guild.GetRole(guildDb.AutoAssignableRoleId);
            if (aar is null)
                return;

            if (currentMember.CheckRoleHierarchy(aar) > 0 && !aar.IsManaged && memberRoles.All(x => x.Id != aar.Id))
                await member.GrantRoleAsync(aar);
        }

        private async Task GuildMemberUpdatedAsync(GuildMemberUpdateEventArgs args)
        {
            RiasBot.Members[args.Member.Id] = args.Member;
            
            // we care only about the mute role
            if (args.RolesBefore.Count == args.RolesAfter.Count)
                return;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == args.Guild.Id);

            if (guildDb is null) return;

            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == args.Guild.Id && x.UserId == args.Member.Id);
            if (userGuildDb is null) return;

            userGuildDb.IsMuted = args.Member.Roles.FirstOrDefault(x => x.Id == guildDb.MuteRoleId) != null;
            await db.SaveChangesAsync();
        }
        
        public static string ReplacePlaceholders(DiscordUser user, string message)
        {
            var sb = new StringBuilder(message)
                
                .Replace("%user%", user.FullName())
                .Replace("%user_id%", user.Id.ToString())
                .Replace("%avatar%", user.GetAvatarUrl(ImageFormat.Auto));

            if (user is DiscordMember member)
            {
                sb.Replace("%mention%", member.Mention)
                    .Replace("%guild%", member.Guild.Name)
                    .Replace("%server%", member.Guild.Name);
            }

            return sb.ToString();
        }
        
        public async Task<EvaluationDetails?> EvaluateAsync(RiasCommandContext context, string code)
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
                "Rias.Core", "Rias.Core.Extensions", "Rias.Core.Database", "Qmmands"
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

                if (result.ReturnValue is null)
                    return null;

                var evaluationDetails = new EvaluationDetails
                {
                    CompilationTime = compilationTime,
                    ExecutionTime = sw.Elapsed,
                    Code = code,
                    IsCompiled = true,
                    Success = true
                };

                var returnValue = result.ReturnValue;
                var type = result.ReturnValue.GetType();

                switch (returnValue)
                {
                    case string str:
                        evaluationDetails.Result = str;
                        evaluationDetails.ReturnType = type.Name;
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
                        evaluationDetails.Result = returnValue.ToString();
                        evaluationDetails.ReturnType = type.Name;
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
        
        private readonly string[] _codeLanguages = { "cs", "csharp" };
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

    public class EvaluationDetails
    {
        public TimeSpan? CompilationTime { get; set; }
        public TimeSpan? ExecutionTime { get; set; }
        public string? Code { get; set; }
        public string? Result { get; set; }
        public string? ReturnType { get; set; }
        public bool IsCompiled { get; set; }
        public bool Success { get; set; }
        public string? Exception { get; set; }
    }
}