using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services.Commons;
using Serilog;

namespace Rias.Services
{
    [AutoStart]
    public class CommandHandlerService : RiasService
    {
        public readonly CommandStatistics CommandStatistics = new CommandStatistics();
        
        private readonly CommandService _commandService;
        private readonly BotService _botService;
        private readonly XpService _xpService;
        
        private readonly ConcurrentHashSet<string> _cooldowns = new ConcurrentHashSet<string>();
        private readonly string _commandsPath = Path.Combine(Environment.CurrentDirectory, "data/commands.xml");

        private List<Type> _typeParsers = new List<Type>();

        public CommandHandlerService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _commandService = serviceProvider.GetRequiredService<CommandService>();
            _botService = serviceProvider.GetRequiredService<BotService>();
            _xpService = serviceProvider.GetRequiredService<XpService>();
            
            LoadCommands();
            LoadTypeParsers();

            RiasBot.Client.MessageCreated += MessageCreatedAsync;
        }

        private void LoadCommands()
        {
            var sw = Stopwatch.StartNew();
            var commandsXml = XElement.Load(_commandsPath);
            var modulesInfo = LoadXmlModules(commandsXml.Elements("module")).ToList();
            
            if (modulesInfo is null)
            {
                throw new KeyNotFoundException("The modules node array couldn't be loaded");
            }

            var assembly = Assembly.GetAssembly(typeof(RiasBot));
            _commandService.AddModules(assembly, null, module => SetUpModule(module, modulesInfo));
            
#if RELEASE
            var testModule = _commandService.GetAllModules().FirstOrDefault(x => string.Equals(x.Name, "Test"));
            if (testModule is not null)
                _commandService.RemoveModule(testModule);
#endif

            sw.Stop();
            Log.Information($"Commands loaded: {sw.ElapsedMilliseconds} ms");
        }
        
#pragma warning disable 8604
        private IEnumerable<ModuleInfo> LoadXmlModules(IEnumerable<XElement> modulesElement)
            => modulesElement.Select(moduleElement =>
                new ModuleInfo
                {

                    Name = moduleElement.Element("name")!.Value,
                    Aliases = moduleElement.Element("aliases")?.Value,
                    Commands = LoadXmlCommands(moduleElement).ToList(),
                    Submodules = moduleElement.Element("submodules") is not null
                        ? LoadXmlModules(moduleElement.Element("submodules")!.Elements("submodule")).ToList()
                        : null
                });

        private IEnumerable<CommandInfo> LoadXmlCommands(XElement moduleElement)
            => moduleElement.Element("commands")!
                .Elements("command")
                .Select(commandElement =>
                    new CommandInfo
                    {
                        Aliases = commandElement.Element("aliases")?.Value,
                        Description = commandElement.Element("description")!.Value,
                        Remarks = commandElement.Element("remarks")!.Elements("remark")!.Select(x => x.Value).ToList()
                    });
#pragma warning restore 8604

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
            
            if (moduleInfo.Commands is null || moduleInfo.Commands.Count == 0)
                return;
            
            SetUpCommands(module, moduleInfo.Commands);

            if (moduleInfo.Submodules is null)
                return;
            
            foreach (var submodule in module.Submodules)
            {
                SetUpModule(submodule, moduleInfo.Submodules);
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

            var typeParserInterface = _commandService.GetType().Assembly.GetTypes()
                .FirstOrDefault(x => x.Name == parserInterface);

            if (typeParserInterface is null)
                throw new NullReferenceException(parserInterface);

            var assembly = typeof(RiasBot).Assembly;
            _typeParsers = assembly!.GetTypes()
                .Where(x => typeParserInterface.IsAssignableFrom(x)
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract)
                .ToList();

            foreach (var typeParser in _typeParsers)
            {
                var methodInfo = typeof(CommandService).GetMethods()
                    .First(m => m.Name == "AddTypeParser" && m.IsGenericMethodDefinition);

                var targetBase = typeParser.BaseType ?? typeParser;
                var targetType = targetBase.GetGenericArguments()[0];

                var genericMethodInfo = methodInfo.MakeGenericMethod(targetType);
                genericMethodInfo.Invoke(_commandService, new[] { Activator.CreateInstance(typeParser), false });
            }
        }
        
        private async Task MessageCreatedAsync(DiscordClient client, MessageCreateEventArgs args)
        {
            if (args.Message.MessageType != MessageType.Default) return;
            if (args.Message.Author.IsBot) return;
            
            await RunTaskAsync(ExecuteAsync(client, args));
        }

        private async Task ExecuteAsync(DiscordClient client, MessageCreateEventArgs args)
        {
            if (args.Guild is not null)
            {
                var member = await args.Guild.GetMemberAsync(args.Author.Id);
                await RunTaskAsync(_botService.AddAssignableRoleAsync(member));
                await RunTaskAsync(_xpService.AddUserXpAsync(args.Author));
                await RunTaskAsync(_xpService.AddGuildUserXpAsync(member, args.Channel));
                
                var channelPermissions = args.Guild.CurrentMember.PermissionsIn(args.Channel);
                if (!channelPermissions.HasPermission(Permissions.SendMessages))
                    return;
            }
            
            var prefix = await GetGuildPrefixAsync(args.Guild);
            if (!CommandUtilities.HasPrefix(args.Message.Content, prefix, StringComparison.InvariantCultureIgnoreCase, out var output))
            {
                if (client.CurrentUser is null)
                    return;

                if (!CommandUtilities.HasPrefix(args.Message.Content, client.CurrentUser.Username, StringComparison.InvariantCultureIgnoreCase, out output)
                    && !args.Message.HasMentionPrefix(client.CurrentUser, out output))
                    return;
            }

            if (await CheckUserBan(args.Author) && args.Author.Id != Credentials.MasterId)
                return;
            
            var context = new RiasCommandContext(RiasBot, args.Message, prefix);
            var result = await _commandService.ExecuteAsync(output, context);
            
            if (result.IsSuccessful)
            {
                if (args.Guild is not null
                    && args.Guild.CurrentMember.GetPermissions().HasPermission(Permissions.ManageMessages)
                    && await CheckGuildCommandMessageDeletion(args.Guild)
                    && !string.Equals(context.Command.Name, "prune"))
                {
                    await args.Message.DeleteAsync();
                }
                
                CommandStatistics.IncrementExecutedCommand();
                await RunTaskAsync(CommandStatistics.AddCommandTimestampAsync(DateTime.UtcNow));
                return;
            }
            
            CommandStatistics.IncrementAttemptedCommand();

            switch (result)
            {
                case OverloadsFailedResult overloadsFailedResult:
                    await RunTaskAsync(SendFailedResultsAsync(context, overloadsFailedResult.FailedOverloads.Values));
                    break;
                case ChecksFailedResult :
                case TypeParseFailedResult:
                case ArgumentParseFailedResult:
                    await RunTaskAsync(SendFailedResultsAsync(context, new[] { (FailedResult)result }));
                    break;
                case CommandOnCooldownResult commandOnCooldownResult:
                    await RunTaskAsync(SendCommandOnCooldownMessageAsync(context, commandOnCooldownResult));
                    break;
            }
        }

        private Task SendFailedResultsAsync(RiasCommandContext context, IEnumerable<FailedResult> failedResults)
        {
            var guildId = context.Guild?.Id;
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ErrorColor,
                Title = GetText(guildId, Localization.ServiceCommandNotExecuted)
            };

            var reasons = new List<string>();
            var checksHashSet = new HashSet<Type>();

            var parsedPrimitiveType = false;
            var areTooManyArguments = false;
            var areTooLessArguments = false;
            
            foreach (var failedResult in failedResults)
            {
                switch (failedResult)
                {
                    case ChecksFailedResult checksFailedResult:
                        foreach (var (check, result) in checksFailedResult.FailedChecks)
                        {
                            if (checksHashSet.Contains(check.GetType()))
                                continue;
                            
                            reasons.Add(result.Reason);
                            checksHashSet.Add(check.GetType());
                        }
                        break;
                    case TypeParseFailedResult typeParseFailedResult:
                        if (_typeParsers.Any(x => x.BaseType!.GetGenericArguments()[0] == typeParseFailedResult.Parameter.Type))
                        {
                            reasons.Add(typeParseFailedResult.Reason);
                        }
                        else if (!parsedPrimitiveType)
                        {
                            reasons.Add(GetText(guildId, Localization.TypeParserPrimitiveType, context.Prefix, typeParseFailedResult.Parameter.Command.Name));
                            parsedPrimitiveType = true;
                        }
                        
                        break;
                    case ArgumentParseFailedResult argumentParseFailedResult:
                        var arguments = CountArguments(context.RawArguments);
                        var parameters = argumentParseFailedResult.Command.Parameters;

                        if (!areTooLessArguments && arguments < parameters.Count)
                        {
                            reasons.Add(GetText(guildId, Localization.ServiceCommandLessArguments, context.Prefix, argumentParseFailedResult.Command.Name));
                            areTooLessArguments = true;
                        }
                        
                        if (!areTooManyArguments && arguments > parameters.Count)
                        {
                            reasons.Add(GetText(guildId, Localization.ServiceCommandManyArguments, context.Prefix, argumentParseFailedResult.Command.Name));
                            areTooManyArguments = true;
                        }
                        
                        break;
                }
            }

            if (reasons.Count == 0)
                return Task.CompletedTask;

            embed.WithDescription($"**{GetText(guildId, reasons.Count == 1 ? Localization.CommonReason : Localization.CommonReasons)}**:\n" +
                                  string.Join("\n", reasons.Select(x => $"• {x}")));
            return context.Channel.SendMessageAsync(embed: embed);
        }
        
        private async Task SendCommandOnCooldownMessageAsync(RiasCommandContext context, CommandOnCooldownResult result)
        {
            var (cooldown, retryAfter) = result.Cooldowns[0];
            var cooldownKey = (BucketType)cooldown.BucketType switch
            {
                BucketType.Guild => GenerateCooldownKey(context.Command.Name, context.Guild!.Id),
                BucketType.User => GenerateCooldownKey(context.Command.Name, context.User.Id),
                BucketType.Member => GenerateCooldownKey(context.Command.Name, context.Guild!.Id, context.User.Id),
                BucketType.Channel => GenerateCooldownKey(context.Command.Name, context.Channel.Id),
                _ => string.Empty
            };
            
            if (_cooldowns.Contains(cooldownKey))
                return;

            _cooldowns.Add(cooldownKey);
            
            retryAfter += TimeSpan.FromSeconds(1);
            await context.Channel.SendErrorMessageAsync(GetText(
                context.Guild?.Id,
                Localization.ServiceCommandCooldown,
                retryAfter.Humanize(culture: new CultureInfo(Localization.GetGuildLocale(context.Guild?.Id)), minUnit: TimeUnit.Second)));
            
            await Task.Delay(retryAfter);
            _cooldowns.TryRemove(cooldownKey);
        }
        
        private async Task<string> GetGuildPrefixAsync(DiscordGuild? guild)
        {
            if (guild is null)
                return Credentials.Prefix;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var prefix = (await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id))?.Prefix;
            
            return !string.IsNullOrEmpty(prefix) ? prefix : Credentials.Prefix;
        }
        
        private async Task<bool> CheckGuildCommandMessageDeletion(DiscordGuild guild)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return (await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id))?.DeleteCommandMessage ?? false;
        }
        
        private async Task<bool> CheckUserBan(DiscordUser user)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return (await db.Users.FirstOrDefaultAsync(x => x.UserId == user.Id))?.IsBanned ?? false;
        }
        
        private string GenerateCooldownKey(string name, ulong id, ulong? secondId = null)
            => secondId.HasValue ? $"{name}_{id}_{secondId}" : $"{name}_{id}";
        
        private int CountArguments(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                return 0;

            var count = 0;
            var quoteOpened = false;

            for (var i = 0; i < arguments.Length; i++)
            {
                var character = arguments[i];
                
                if (character == '"' && !quoteOpened)
                    quoteOpened = true;

                if (char.IsWhiteSpace(character))
                {
                    if (quoteOpened && i > 0 && arguments[i - 1] == '"')
                        quoteOpened = false;

                    if (quoteOpened)
                        continue;

                    if (i > 0 && char.IsWhiteSpace(arguments[i - 1]))
                        continue;

                    count++;    
                }
            }

            if (!char.IsWhiteSpace(arguments[^1]))
                count++;

            return count;
        }
    }
}