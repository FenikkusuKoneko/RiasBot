using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Serilog;

namespace Rias.Core.Services
{
    public class CommandHandlerService : RiasService
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandService _service;
        private readonly Resources _resources;
        private readonly BotService _botService;
        private readonly XpService _xpService;
        private readonly CooldownService _cooldownService;

        private readonly string _commandsPath = Path.Combine(Environment.CurrentDirectory, "data/commands.json");
        public static int CommandsAttempted;
        public static int CommandsExecuted;

        public CommandHandlerService(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _service = services.GetRequiredService<CommandService>();
            _resources = services.GetRequiredService<Resources>();
            _botService = services.GetRequiredService<BotService>();
            _xpService = services.GetRequiredService<XpService>();
            _cooldownService = services.GetRequiredService<CooldownService>();

            LoadCommands();
            LoadTypeParsers();

            _client.MessageReceived += MessageReceivedAsync;
        }

        private void LoadCommands()
        {
            var sw = Stopwatch.StartNew();
            var modulesInfo = JObject.Parse(File.ReadAllText(_commandsPath))
                .SelectToken("modules")?
                .ToObject<List<ModuleInfo>>();

            if (modulesInfo is null)
            {
                throw new KeyNotFoundException("The modules node array couldn't be loaded");
            }

            var assembly = Assembly.GetAssembly(typeof(Rias));
            _service.AddModules(assembly, null, module => SetUpModule(module, modulesInfo));

            sw.Stop();
            Log.Information($"Commands loaded: {sw.ElapsedMilliseconds} ms");
        }

        private void SetUpModule(ModuleBuilder module, IReadOnlyList<ModuleInfo> modulesInfo)
        {
            if (string.IsNullOrEmpty(module.Name))
                return;
            
            var moduleInfo = modulesInfo.FirstOrDefault(x => string.Equals(x.Name, module.Name, StringComparison.InvariantCultureIgnoreCase));
            if (moduleInfo is null)
                return;

            if (!string.IsNullOrEmpty(moduleInfo.Aliases))
            {
                foreach (var moduleAlias in moduleInfo.Aliases.Split(" "))
                {
                    module.AddAlias(moduleAlias);
                }
            }

            if (!moduleInfo.Commands.Any())
                return;
            
            SetUpCommands(module, moduleInfo.Commands.ToList());

            foreach (var submodule in module.Submodules)
            {
                SetUpModule(submodule, moduleInfo.Submodules.ToList());
            }
        }

        private void SetUpCommands(ModuleBuilder module, IReadOnlyList<CommandInfo> commandsInfo)
        {
            foreach (var command in module.Commands)
            {
                Func<CommandInfo, bool> predicate;
                if (command.Aliases.Count != 0)
                {
                    predicate = x => !string.IsNullOrEmpty(x.Aliases) && x.Aliases.Split(" ")
                                         .Any(y => string.Equals(y, command.Aliases.First(), StringComparison.InvariantCultureIgnoreCase));
                }
                else
                {
                    predicate = x => string.IsNullOrEmpty(x.Aliases);
                }

                var commandInfo = commandsInfo.FirstOrDefault(predicate);
                if (commandInfo is null) continue;

                if (!string.IsNullOrEmpty(commandInfo.Aliases))
                {
                    foreach (var commandAlias in commandInfo.Aliases.Split(" "))
                    {
                        if (command.Aliases.Contains(commandAlias))
                            continue;

                        if (!Creds.IsGlobal && string.Equals(commandAlias, "hearts", StringComparison.Ordinal))
                            continue;

                        command.AddAlias(commandAlias);
                    }
                }

                command.Description = commandInfo.Description;
                command.Remarks = string.Join("\n", commandInfo.Remarks!);
            }
        }

        private void LoadTypeParsers()
        {
            const string parserInterface = "ITypeParser";

            var typeParserInterface = _service.GetType().Assembly.GetTypes()
                .FirstOrDefault(x => x.Name == parserInterface)?.GetTypeInfo();

            if (typeParserInterface is null)
                throw new NullReferenceException(parserInterface);

            var assembly = typeof(Rias).Assembly;
            var typeParsers = assembly!.GetTypes()
                .Where(x => typeParserInterface.IsAssignableFrom(x)
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract)
                .ToArray();

            foreach (var typeParser in typeParsers)
            {
                var methodInfo = typeof(CommandService).GetMethods()
                    .First(m => m.Name == "AddTypeParser" && m.IsGenericMethodDefinition);

                var targetBase = typeParser.BaseType ?? typeParser;
                var targetType = targetBase.GetGenericArguments()[0];

                var genericMethodInfo = methodInfo.MakeGenericMethod(targetType);
                genericMethodInfo.Invoke(_service, new[] { Activator.CreateInstance(typeParser), false });
            }
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage)) return;
            if (userMessage.Author.IsBot) return;
            
            var guildChannel = userMessage.Channel as SocketGuildChannel;
            if (guildChannel != null)
            {
                await RunTaskAsync(_botService.AddAssignableRoleAsync((SocketGuildUser) userMessage.Author));
                await RunTaskAsync(_xpService.AddUserXpAsync((SocketGuildUser) userMessage.Author));
                await RunTaskAsync(_xpService.AddGuildUserXpAsync((SocketGuildUser) userMessage.Author, userMessage.Channel));
            }

            var prefix = await GetGuildPrefixAsync(guildChannel?.Guild);
            if (CommandUtilities.HasPrefix(userMessage.Content, !string.IsNullOrEmpty(prefix) ? prefix : Creds.Prefix, out var output))
            {
                await RunTaskAsync(ExecuteCommandAsync(userMessage, guildChannel, prefix, output));
                return;
            }

            if (_client.CurrentUser is null)
                return;
            
            if (CommandUtilities.HasPrefix(userMessage.Content, $"{_client.CurrentUser.Username} ", StringComparison.InvariantCultureIgnoreCase, out output))
            {
                await RunTaskAsync(ExecuteCommandAsync(userMessage, guildChannel, prefix, output));
                return;
            }

            var index = userMessage.Content.IndexOf(' ');
            if (index < 1) return;
            
            var mention = userMessage.Content[..index];
            if (MentionUtils.TryParseUser(mention, out var id))
            {
                if (id == _client.CurrentUser.Id)
                    await RunTaskAsync(ExecuteCommandAsync(userMessage, guildChannel, prefix, userMessage.Content[index..].TrimStart())); 
            }
        }

        private async Task ExecuteCommandAsync(SocketUserMessage userMessage, SocketGuildChannel? channel, string prefix, string output)
        {
            if (await CheckUserBan(userMessage.Author) && userMessage.Author.Id != Creds.MasterId)
                return;
            
            var channelPermissions = channel?.Guild.CurrentUser.GetPermissions(channel);

            if (channelPermissions.HasValue && !channelPermissions.Value.SendMessages)
                return;

            var context = new RiasCommandContext(_client, userMessage, Services, prefix);
            var result = await _service.ExecuteAsync(output, context);

            CommandsAttempted++;
            if (result.IsSuccessful)
            {
                if (channel != null &&
                    channel.Guild.CurrentUser.GuildPermissions.ManageMessages &&
                    await CheckGuildCommandMessageDeletion(channel.Guild) &&
                    !string.Equals(context.Command.Name, "prune", StringComparison.Ordinal))
                {
                    await userMessage.DeleteAsync();
                }
                
                CommandsExecuted++;
            }

            switch (result)
            {
                case ChecksFailedResult failedResult:
                    await RunTaskAsync(SendErrorResultMessageAsync(context, failedResult));
                    break;
                case CommandOnCooldownResult commandOnCooldownResult:
                    await RunTaskAsync(SendCommandOnCooldownMessageAsync(context, commandOnCooldownResult));
                    break;
                case TypeParseFailedResult typeParseFailedResult:
                    if (typeParseFailedResult.Reason.StartsWith('#'))
                        await RunTaskAsync(SendTypeParseFailedResultAsync(context, typeParseFailedResult));
                    break;
                case ArgumentParseFailedResult argumentParseFailedResult:
                    await RunTaskAsync(SendArgumentParseFailedResultAsync(context, argumentParseFailedResult));
                    break;
            }
            
            if (result.IsSuccessful) return;
            Log.Logger.Information($"[Command] \"{context.Command?.Name}\" (attempted - {result.GetType()})\n" +
                                   $"\t\t[Arguments] \"{string.Join(" ", context.Arguments)}\"\n" +
                                   $"\t\t[User] \"{context.User}\" ({context.User.Id})\n" +
                                   $"\t\t[Channel] \"{context.Channel.Name}\" ({context.Channel.Id})\n" +
                                   $"\t\t[Guild] \"{context.Guild?.Name ?? "DM"}\" ({context.Guild?.Id ?? 0})");
        }

        private Task SendErrorResultMessageAsync(RiasCommandContext context, ChecksFailedResult result)
        {
            var guildId = context.Guild?.Id;
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ErrorColor,
                Title = GetText(guildId, "Service", "CommandNotExecuted")
            };

            var failedChecks = result.FailedChecks;
            (CheckAttribute? check, CheckResult? checkResult) = (null, null);

            var description = GetText(guildId, "Common", "Reason");
            if (failedChecks.Count > 1)
            {
                description = GetText(guildId, "Common", "Reasons");
                (check, checkResult) = failedChecks.FirstOrDefault(x => x.Check is ContextAttribute);
            }

            embed.WithDescription(check is null
                ? $"**{description}**:\n{string.Join("\n", failedChecks.Select(x => x.Result.Reason))}"
                : $"**{description}**:\n{checkResult?.Reason}");
            return context.Channel.SendMessageAsync(embed);
        }

        private async Task SendCommandOnCooldownMessageAsync(RiasCommandContext context, CommandOnCooldownResult result)
        {
            var (cooldown, retryAfter) = result.Cooldowns[0];
            var cooldownKey = (BucketType) cooldown.BucketType switch
            {
                BucketType.Guild => _cooldownService.GenerateKey(context.Command.Name, context.Guild!.Id),
                BucketType.User => _cooldownService.GenerateKey(context.Command.Name, context.User.Id),
                BucketType.GuildUser => _cooldownService.GenerateKey(context.Command.Name, context.Guild!.Id, ((SocketGuildUser) context.User).Id),
                BucketType.Channel => _cooldownService.GenerateKey(context.Command.Name, context.Channel.Id),
                _ => string.Empty
            };
            
            if (_cooldownService.Has(cooldownKey))
                return;

            _cooldownService.Add(cooldownKey);
            
            await context.Channel.SendErrorMessageAsync(GetText(context.Guild?.Id, "Service", "CommandCooldown",
                retryAfter.Humanize(culture: _resources.GetGuildCulture(context.Guild?.Id))));
            
            await Task.Delay(retryAfter);
            _cooldownService.Remove(cooldownKey);
        }

        private Task SendTypeParseFailedResultAsync(RiasCommandContext context, TypeParseFailedResult result)
        {
            string? prefix = null;
            var reason = result.Reason;
            SplitPrefixKey(ref prefix, ref reason);
            return context.Channel.SendErrorMessageAsync(GetText(context.Guild?.Id, prefix, reason));
        }
        
        private async Task SendArgumentParseFailedResultAsync(RiasCommandContext context, ArgumentParseFailedResult result)
        {
            var guildId = context.Guild?.Id;
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ErrorColor,
                Title = GetText(guildId, "Service", "CommandNotExecuted")
            };

            var rawArguments = Regex.Matches(result.RawArguments, @"\w+|""[\w\s]*""");
            if (rawArguments.Count == result.Command.Parameters.Count)
                return;
            
            if (rawArguments.Count < result.Command.Parameters.Count)
                embed.WithDescription(GetText(guildId, "Service", "CommandLessArguments",
                    await GetGuildPrefixAsync(context.Guild), result.Command.Name));
            
            if (rawArguments.Count > result.Command.Parameters.Count)
                embed.WithDescription(GetText(guildId, "Service", "CommandManyArguments",
                    await GetGuildPrefixAsync(context.Guild), result.Command.Name));
            
            await context.Channel.SendMessageAsync(embed);
        }
        
        private async Task<string> GetGuildPrefixAsync(SocketGuild? guild)
        {
            if (guild is null)
                return Creds.Prefix;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var prefix = (await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id))?.Prefix;
            
            return !string.IsNullOrEmpty(prefix) ? prefix : Creds.Prefix;
        }

        private async Task<bool> CheckGuildCommandMessageDeletion(SocketGuild guild)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return (await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id))?.DeleteCommandMessage ?? false;
        }
        
        private async Task<bool> CheckUserBan(SocketUser user)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return (await db.Users.FirstOrDefaultAsync(x => x.UserId == user.Id))?.IsBanned ?? false;
        }
        
        private class ModuleInfo
        {
            public string? Name { get; set; }
            public string? Aliases { get; set; }
            public IEnumerable<CommandInfo>? Commands { get; set; }
            public IEnumerable<ModuleInfo>? Submodules { get; set; }
        }

        private class CommandInfo
        {
            public string? Aliases { get; set; }
            public string? Description { get; set; }
            public IEnumerable<string>? Remarks { get; set; }
        }
    }
}