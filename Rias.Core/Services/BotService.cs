using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;
using Disqord.Rest;
using Disqord.Sharding;
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
            RiasBot.ShardReady += ShardReadyAsync;
            RiasBot.MemberJoined += MemberJoinedAsync;
            RiasBot.MemberLeft += MemberLeftAsync;
        }

        private readonly ConcurrentDictionary<DiscordClientBase, bool> _shardsReady = new ConcurrentDictionary<DiscordClientBase, bool>();
        private readonly ConcurrentDictionary<Snowflake, RestWebhookClient> _webhooks = new ConcurrentDictionary<Snowflake, RestWebhookClient>();

        private async Task MemberJoinedAsync(MemberJoinedEventArgs args)
        {
            var member = args.Member;
            if (RiasBot.CurrentUser != null && member.Id == RiasBot.CurrentUser.Id)
                return;

            await RunTaskAsync(AddAssignableRoleAsync(member));

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == member.Guild.Id);
            await SendGreetMessageAsync(guildDb, member);

            var currentUser = member.Guild.CurrentMember;
            if (!currentUser.Permissions.ManageRoles) return;

            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id && x.UserId == member.Id);
            if (userGuildDb is null) return;
            if (!userGuildDb.IsMuted) return;

            var role = member.Guild.GetRole(guildDb?.MuteRoleId ?? 0)
                       ?? member.Guild.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, MuteService.MuteRole)).Value;
            
            if (role != null)
            {
                await member.GrantRoleAsync(role.Id);
            }
            else
            {
                userGuildDb.IsMuted = false;
                await db.SaveChangesAsync();
            }
        }
        
        private async Task SendGreetMessageAsync(GuildsEntity guildDb, CachedMember member)
        {
            if (guildDb is null) return;
            if (!guildDb.GreetNotification) return;
            if (string.IsNullOrEmpty(guildDb.GreetMessage)) return;
            if (guildDb.GreetWebhookId == 0) return;

            var guild = member.Guild;
            var currentMember = guild.CurrentMember;
            if (!currentMember.Permissions.ManageWebhooks)
                return;

            var guildWebhook = await guild.GetWebhookAsync(guildDb.GreetWebhookId);
            if (_webhooks.TryGetValue(guildDb.GreetWebhookId, out var webhook))
            {
                if (guildWebhook is null)
                {
                    _webhooks.TryRemove(guildDb.GreetWebhookId, out _);
                    webhook.Dispose();
                    await DisableGreetAsync(guild);

                    return;
                }
            }
            else
            {
                if (guildWebhook != null)
                {
                    webhook = new RestWebhookClient(guildWebhook);
                    _webhooks.TryAdd(guildWebhook.Id, webhook);
                }
                else
                {
                    await DisableGreetAsync(guild);
                    return;
                }
            }
            
            if (webhook is null)
                return;
            
            var greetMsg = ReplacePlaceholders(member, guildDb.GreetMessage);
            if (RiasUtilities.TryParseEmbed(greetMsg, out var embed))
                await webhook.ExecuteAsync(embeds: new[] {embed.Build()});
            else
                await webhook.ExecuteAsync(greetMsg);
        }
        
        private async Task DisableGreetAsync(CachedGuild guild)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            if (guildDb is null)
                return;
            
            guildDb.GreetNotification = false;
            await db.SaveChangesAsync();
        }
        
        private async Task MemberLeftAsync(MemberLeftEventArgs args)
        {
            if (RiasBot.CurrentUser != null && args.User.Id == RiasBot.CurrentUser.Id)
                return;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == args.Guild.Id);
            await SendByeMessageAsync(guildDb, args.User, args.Guild);
        }

        private async Task SendByeMessageAsync(GuildsEntity guildDb, CachedUser user, CachedGuild guild)
        {
            if (guildDb is null) return;
            if (!guildDb.ByeNotification) return;
            if (string.IsNullOrEmpty(guildDb.ByeMessage)) return;
            if (guildDb.ByeWebhookId == 0) return;
            
            var currentMember = guild.CurrentMember;
            if (!currentMember.Permissions.ManageWebhooks)
                return;
            
            var guildWebhook = await guild.GetWebhookAsync(guildDb.ByeWebhookId);
            if (_webhooks.TryGetValue(guildDb.ByeWebhookId, out var webhook))
            {
                if (guildWebhook is null)
                {
                    _webhooks.TryRemove(guildDb.ByeWebhookId, out _);
                    webhook.Dispose();
                    await DisableByeAsync(guild);

                    return;
                }
            }
            else
            {
                if (guildWebhook != null)
                {
                    webhook = new RestWebhookClient(guildWebhook);
                    _webhooks.TryAdd(guildWebhook.Id, webhook);
                }
                else
                {
                    await DisableByeAsync(guild);
                    return;
                }
            }
            
            if (webhook is null)
                return;
            
            var byeMsg = ReplacePlaceholders(user, guildDb.ByeMessage);
            if (RiasUtilities.TryParseEmbed(byeMsg, out var embed))
                await webhook.ExecuteAsync(embeds: new[] {embed.Build()});
            else
                await webhook.ExecuteAsync(byeMsg);
        }
        
        private async Task DisableByeAsync(CachedGuild guild)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            if (guildDb is null)
                return;
            
            guildDb.ByeNotification = false;
            await db.SaveChangesAsync();
        }

        private static string ReplacePlaceholders(IUser user, string message)
        {
            var sb = new StringBuilder(message)
                
                .Replace("%user%", user.ToString())
                .Replace("%user_id%", user.Id.ToString())
                .Replace("%avatar%", user.GetAvatarUrl());

            if (user is CachedMember member)
            {
                sb.Replace("%mention%", member.Mention)
                    .Replace("%guild%", member.Guild.Name)
                    .Replace("%server%", member.Guild.Name);
            }

            return sb.ToString();
        }
        
        private Task ShardReadyAsync(ShardReadyEventArgs e)
        {
            _shardsReady.AddOrUpdate(e.Client, true, (shardKey, value) => true);
            if (_shardsReady.Count == RiasBot.Shards.Count && _shardsReady.All(x => x.Value))
            {
                RiasBot.ShardReady -= ShardReadyAsync;
                Log.Information("All shards are connected");

                RiasBot.GetRequiredService<MuteService>();
                
                var reactionsService =  RiasBot.GetRequiredService<ReactionsService>();
                reactionsService.WeebUserAgent = $"{RiasBot.CurrentUser.Name}/{Rias.Version}";
                reactionsService.AddWeebUserAgent();
            }

            return Task.CompletedTask;
        }
        
        public async Task AddAssignableRoleAsync(CachedMember member)
        {
            var currentMember = member.Guild.CurrentMember;
            if (!currentMember.Permissions.ManageRoles)
                return;

            if (member.Roles.Count > 1) return;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == member.Guild.Id);
            if (guildDb is null)
                return;

            var aar = member.Guild.GetRole(guildDb.AutoAssignableRoleId);
            if (aar is null)
                return;

            if (currentMember.CheckRoleHierarchy(aar) > 0 && !aar.IsManaged && member.GetRole(aar.Id) is null)
                await member.GrantRoleAsync(aar.Id);
        }
        
        public async Task<EvaluationDetails?> EvaluateAsync(RiasCommandContext context, string code)
        {
            var globals = new RoslynGlobals
            {
                Rias = RiasBot,
                Context = context
            };

            var imports = new[]
            {
                "System", "System.Collections.Generic", "System.Linq", "Disqord", "Disqord.WebSocket",
                "System.Threading.Tasks", "System.Text", "Microsoft.Extensions.DependencyInjection", "System.Net.Http",
                "Rias.Core.Extensions", "Rias.Core.Database", "Qmmands"
            };
            
            var scriptOptions = ScriptOptions.Default.WithReferences(typeof(Rias).Assembly).AddImports(imports);
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
                    Exception = string.Join('\n', diagnostics.Select(x => x.ToString()))
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