using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services.Commons;
using Serilog;
using Victoria;

namespace Rias.Core.Services
{
    public class BotService : RiasService
    {
        private readonly DiscordShardedClient _client;
        private readonly LavaNode<MusicPlayer> _lavalink;

        public BotService(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _lavalink = services.GetRequiredService<LavaNode<MusicPlayer>>();

            _client.UserJoined += UserJoinedAsync;
            _client.UserLeft += UserLeftAsync;
            _client.GuildMemberUpdated += GuildMemberUpdatedAsync;

            _client.ShardReady += ShardReadyAsync;
            _client.ShardDisconnected += ShardDisconnectedAsync;
        }

        private readonly ConcurrentDictionary<DiscordSocketClient, bool> _shardsReady = new ConcurrentDictionary<DiscordSocketClient, bool>();
        private readonly ConcurrentDictionary<ulong, DiscordWebhookClient> _webhooks = new ConcurrentDictionary<ulong, DiscordWebhookClient>();

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            if (user.Id == _client.CurrentUser.Id)
                return;

            await RunTaskAsync(AddAssignableRoleAsync(user));

            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(g => g.GuildId == user.Guild.Id);
            await SendGreetMessageAsync(guildDb, user);

            var currentUser = user.Guild.CurrentUser;
            if (!currentUser.GuildPermissions.ManageRoles) return;

            var userGuildDb = db.GuildUsers.Where(x => x.GuildId == user.Guild.Id).FirstOrDefault(x => x.UserId == user.Id);
            if (userGuildDb is null) return;
            if (!userGuildDb.IsMuted) return;

            var role = user.Guild.GetRole(guildDb?.MuteRoleId ?? 0)
                       ?? user.Guild.Roles.FirstOrDefault(x => x.Name == MuteService.MuteRole);
            if (role != null)
            {
                await user.AddRoleAsync(role);
            }
            else
            {
                userGuildDb.IsMuted = false;
                await db.SaveChangesAsync();
            }
        }

        private async Task SendGreetMessageAsync(Guilds guildDb, SocketGuildUser user)
        {
            if (guildDb is null) return;
            if (!guildDb.GreetNotification) return;
            if (string.IsNullOrEmpty(guildDb.GreetMessage)) return;
            if (guildDb.GreetWebhookId == 0) return;

            var guild = user.Guild;
            var currentUser = guild.CurrentUser;
            if (!currentUser.GuildPermissions.ManageWebhooks)
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
                    webhook = new DiscordWebhookClient(guildWebhook);
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
            
            var greetMsg = ReplacePlaceholders(user, guildDb.GreetMessage);
            if (RiasUtils.TryParseEmbed(greetMsg, out var embed))
                await webhook.SendMessageAsync(embeds: new[] {embed.Build()});
            else
                await webhook.SendMessageAsync(greetMsg);
        }

        private async Task DisableGreetAsync(SocketGuild guild)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb is null)
                return;
            
            guildDb.GreetNotification = false;
            await db.SaveChangesAsync();
        }

        private async Task UserLeftAsync(SocketGuildUser user)
        {
            if (_client.CurrentUser != null && user.Id == _client.CurrentUser.Id && _lavalink.IsConnected)
            {
                if (!_lavalink.TryGetPlayer(user.Guild, out var player))
                    return;

                await player.LeaveAndDisposeAsync(false);
                return;
            }

            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(g => g.GuildId == user.Guild.Id);
            await SendByeMessageAsync(guildDb, user);
        }

        private async Task SendByeMessageAsync(Guilds guildDb, SocketGuildUser user)
        {
            if (guildDb is null) return;
            if (!guildDb.ByeNotification) return;
            if (string.IsNullOrEmpty(guildDb.ByeMessage)) return;
            if (guildDb.ByeWebhookId == 0) return;
            
            var guild = user.Guild;
            var currentUser = guild.CurrentUser;
            if (!currentUser.GuildPermissions.ManageWebhooks)
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
                    webhook = new DiscordWebhookClient(guildWebhook);
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
            if (RiasUtils.TryParseEmbed(byeMsg, out var embed))
                await webhook.SendMessageAsync(embeds: new[] {embed.Build()});
            else
                await webhook.SendMessageAsync(byeMsg);
        }
        
        private async Task DisableByeAsync(SocketGuild guild)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb is null)
                return;
            
            guildDb.ByeNotification = false;
            await db.SaveChangesAsync();
        }

        private static string ReplacePlaceholders(SocketGuildUser user, string message)
            => new StringBuilder(message)
                .Replace("%mention%", user.Mention)
                .Replace("%user%", user.ToString())
                .Replace("%user_id%", user.Id.ToString())
                .Replace("%guild%", user.Guild.Name)
                .Replace("%server%", user.Guild.Name)
                .Replace("%avatar%", user.GetRealAvatarUrl())
                .ToString();

        public async Task AddAssignableRoleAsync(SocketGuildUser user)
        {
            var currentUser = user.Guild.CurrentUser;
            if (!currentUser.GuildPermissions.ManageRoles)
                return;

            if (user.Roles.Count > 1) return;

            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == user.Guild.Id);
            if (guildDb is null)
                return;

            var aar = user.Guild.GetRole(guildDb.AutoAssignableRoleId);
            if (aar is null)
                return;

            if (currentUser.CheckRoleHierarchy(aar) > 0 && !aar.IsManaged && user.Roles.All(x => x.Id != aar.Id))
                await user.AddRoleAsync(aar);
        }

        private async Task GuildMemberUpdatedAsync(SocketGuildUser oldUser, SocketGuildUser newUser)
        {
            // we care only about the mute role
            if (oldUser.Roles.Count == newUser.Roles.Count)
                return;

            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(g => g.GuildId == newUser.Guild.Id);

            if (guildDb is null) return;

            var userGuildDb = db.GuildUsers.FirstOrDefault(x => x.GuildId == newUser.Guild.Id && x.UserId == newUser.Id);
            if (userGuildDb is null) return;

            userGuildDb.IsMuted = newUser.Roles.FirstOrDefault(r => r.Id == guildDb.MuteRoleId) != null;
            await db.SaveChangesAsync();
        }

        private async Task ShardReadyAsync(DiscordSocketClient shard)
        {
            _shardsReady.AddOrUpdate(shard, true, (shardKey, value) => true);

            if (_shardsReady.Count == _client.Shards.Count && _shardsReady.All(x => x.Value))
            {
                _client.ShardReady -= ShardReadyAsync;
                Log.Information("All shards are ready");

                Services.GetRequiredService<MuteService>();
                await _lavalink.ConnectAsync();
            }
        }

        private async Task ShardDisconnectedAsync(Exception ex, DiscordSocketClient shard)
        {
            _shardsReady.TryUpdate(shard, false,  true);

            foreach (var player in _lavalink.Players)
            {
                await player.LeaveAndDisposeAsync(false);
            }
        }

        public async Task<EvaluationDetails?> EvaluateAsync(RiasCommandContext context, string code)
        {
            var references = new[]
            {
                typeof(Rias).Assembly,
            };
            
            var globals = new RoslynGlobals
            {
                Context = context,
                Client = _client,
                SocketClient = context.Client,
                Services = Services
            };

            var imports = new[]
            {
                "System", "System.Collections.Generic", "System.Linq", "Discord", "Discord.WebSocket",
                "System.Threading.Tasks", "System.Text", "Microsoft.Extensions.DependencyInjection", "System.Net.Http",
                "Rias.Core.Extensions", "Rias.Core.Database", "Qmmands"
            };
            
            var scriptOptions = ScriptOptions.Default.WithReferences(references).AddImports(imports);
            code = SanitizeCode(code);
            
            var sw = Stopwatch.StartNew();
            var script = CSharpScript.Create(code, scriptOptions, typeof(RoslynGlobals));
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
                    IsCompiled = true,
                    Success = false,
                    Exception = ex.Message
                };
            }
            finally
            {
                GC.Collect();
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