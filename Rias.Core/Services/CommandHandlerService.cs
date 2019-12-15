using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
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
                        
#if !(DEBUG || GLOBAL)
                        if (string.Equals(commandAlias, "hearts", StringComparison.Ordinal))
                            continue;
#endif
                        
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
                await RunTaskAsync(_botService.AddAssignableRoleAsync((SocketGuildUser) userMessage.Author));

            await RunTaskAsync(_xpService.AddUserXpAsync(userMessage));
            await RunTaskAsync(_xpService.AddGuildUserXpAsync(userMessage));
            
            var prefix = GetPrefix(guildChannel);
            if (CommandUtilities.HasPrefix(userMessage.Content, !string.IsNullOrEmpty(prefix) ? prefix : Creds.Prefix, out var output))
            {
                await RunTaskAsync(ExecuteCommandAsync(userMessage, guildChannel, output));
                return;
            }

            if (_client.CurrentUser is null)
                return;
            
            if (CommandUtilities.HasPrefix(userMessage.Content, $"{_client.CurrentUser.Username} ", StringComparison.InvariantCultureIgnoreCase, out output)
                || CommandUtilities.HasPrefix(userMessage.Content, $"{_client.CurrentUser.Mention.Replace("!", "")} ", StringComparison.InvariantCultureIgnoreCase, out output))
            {
                await RunTaskAsync(ExecuteCommandAsync(userMessage, guildChannel, output));
            }
        }

        private async Task ExecuteCommandAsync(SocketUserMessage userMessage, SocketGuildChannel? channel, string output)
        {
            if (CheckUserBan(userMessage.Author) && userMessage.Author.Id != Creds.MasterId)
                return;
            
            var channelPermissions = channel?.Guild.CurrentUser.GetPermissions(channel);

            if (channelPermissions.HasValue && !channelPermissions.Value.SendMessages)
                return;

            var context = new RiasCommandContext(_client, userMessage, Services);
            var result = await _service.ExecuteAsync(output, context);

            if (result.IsSuccessful)
            {
                if (channel != null &&
                    channel.Guild.CurrentUser.GuildPermissions.ManageMessages &&
                    CheckGuildCommandMessageDeletion(channel.Guild))
                {
                    await userMessage.DeleteAsync();
                }
                
                CommandsExecuted++;
            }
            
            switch (result)
            {
                case ChecksFailedResult failedResult:
                    await RunTaskAsync(SendErrorResultMessageAsync(context, userMessage, failedResult));
                    break;
                case CommandOnCooldownResult commandOnCooldownResult:
                    await RunTaskAsync(SendCommandOnCooldownMessageAsync(context, commandOnCooldownResult));
                    break;
                case TypeParseFailedResult typeParseFailedResult:
                    if (typeParseFailedResult.Reason.StartsWith('#'))
                        await RunTaskAsync(SendTypeParseFailedResultAsync(context, typeParseFailedResult));
                    break;
            }
        }

        private Task SendErrorResultMessageAsync(RiasCommandContext context, SocketUserMessage userMessage, ChecksFailedResult result)
        {
            var guildId = context.Guild?.Id;
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ErrorColor,
                Title = _resources.GetText(guildId, "Service", "CommandNotExecuted")
            };

            var failedChecks = result.FailedChecks;
            (CheckAttribute? check, CheckResult? checkResult) = (null, null);

            var description = _resources.GetText(guildId, "Common", "Reason");
            if (failedChecks.Count > 1)
            {
                description = _resources.GetText(guildId, "Common", "Reasons");
                (check, checkResult) = failedChecks.FirstOrDefault(x => x.Check is ContextAttribute);
            }

            embed.WithDescription(check is null
                ? $"**{description}**:\n{string.Join("\n", failedChecks.Select(x => x.Result.Reason))}"
                : $"**{description}**:\n{checkResult?.Reason}");
            return userMessage.Channel.SendMessageAsync(embed);
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
            
            await context.Channel.SendErrorMessageAsync(_resources.GetText(context.Guild?.Id, "Service", "CommandCooldown",
                retryAfter.Humanize(culture: _resources.GetGuildCulture(context.Guild?.Id))));
            
            await Task.Delay(retryAfter);
            _cooldownService.Remove(cooldownKey);
        }

        private Task SendTypeParseFailedResultAsync(RiasCommandContext context, TypeParseFailedResult result)
        {
            string? prefix = null;
            var reason = result.Reason;
            SplitPrefixKey(ref prefix, ref reason);
            return context.Channel.SendErrorMessageAsync(_resources.GetText(context.Guild?.Id, prefix, reason));
        }

        private string? GetPrefix(SocketGuildChannel? channel)
        {
            if (channel is null)
                return null;
            
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Guilds.FirstOrDefault(x => x.GuildId == channel.Guild.Id)?.Prefix;
        }

        private bool CheckGuildCommandMessageDeletion(SocketGuild guild)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id)?.DeleteCommandMessage ?? false;
        }
        
        private bool CheckUserBan(SocketUser user)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Users.FirstOrDefault(x => x.UserId == user.Id)?.IsBanned ?? false;
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