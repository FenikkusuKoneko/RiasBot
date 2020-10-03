using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
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
using Serilog;

namespace Rias.Services
{
    [AutoStart]
    public class CommandHandlerService : RiasService
    {
        private readonly CommandService _commandService;
        private readonly BotService _botService;
        private readonly CooldownService _cooldownService;
        private readonly XpService _xpService;
        
        private readonly string _commandsPath = Path.Combine(Environment.CurrentDirectory, "data/commands.xml");

        private List<Type> _typeParsers = new List<Type>();

        public CommandHandlerService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _commandService = serviceProvider.GetRequiredService<CommandService>();
            _botService = serviceProvider.GetRequiredService<BotService>();
            _cooldownService = serviceProvider.GetRequiredService<CooldownService>();
            _xpService = serviceProvider.GetRequiredService<XpService>();
            
            LoadCommands();
            LoadTypeParsers();

            RiasBot.Client.MessageCreated += MessageCreatedAsync;
        }

        public int CommandsAttempted { get; private set; }

        public int CommandsExecuted { get; private set; }

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
            
            if (args.Channel.Type == ChannelType.Text)
            {
                await RunTaskAsync(_botService.AddAssignableRoleAsync((DiscordMember)args.Author));
                await RunTaskAsync(_xpService.AddUserXpAsync(args.Author));
                await RunTaskAsync(_xpService.AddGuildUserXpAsync((DiscordMember)args.Author, args.Channel));
                
                var channelPermissions = args.Channel.Guild.CurrentMember.PermissionsIn(args.Channel);
                if (!channelPermissions.HasPermission(Permissions.SendMessages))
                    return;
            }

            var prefix = await GetGuildPrefixAsync(args.Guild);
            if (CommandUtilities.HasPrefix(args.Message.Content, prefix, out var output))
            {
                await RunTaskAsync(ExecuteCommandAsync(args.Message, args.Channel, prefix, output));
                return;
            }

            if (client.CurrentUser is null)
                return;

            if (CommandUtilities.HasPrefix(args.Message.Content, client.CurrentUser.Username, StringComparison.InvariantCultureIgnoreCase, out output)
                || args.Message.HasMentionPrefix(client.CurrentUser, out output))
                await RunTaskAsync(ExecuteCommandAsync(args.Message, args.Channel, prefix, output));
        }

        private async Task ExecuteCommandAsync(DiscordMessage message, DiscordChannel channel, string prefix, string output)
        {
            if (await CheckUserBan(message.Author) && message.Author.Id != Credentials.MasterId)
                return;

            var context = new RiasCommandContext(RiasBot, message, prefix);
            var result = await _commandService.ExecuteAsync(output, context);
            
            if (result.IsSuccessful)
            {
                if (channel.Type == ChannelType.Text
                    && channel.Guild.CurrentMember.GetPermissions().HasPermission(Permissions.ManageMessages)
                    && await CheckGuildCommandMessageDeletion(channel.Guild)
                    && !string.Equals(context.Command.Name, "prune"))
                {
                    await message.DeleteAsync();
                }
                
                CommandsExecuted++;
                return;
            }
            
            CommandsAttempted++;

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
            var parsedPrimitiveType = false;
            var areTooManyArguments = false;
            var areTooLessArguments = false;
            
            foreach (var failedResult in failedResults)
            {
                switch (failedResult)
                {
                    case ChecksFailedResult checksFailedResult:
                        reasons.AddRange(checksFailedResult.FailedChecks.Select(x => x.Result.Reason));
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
                        var rawArguments = Regex.Matches(argumentParseFailedResult.RawArguments, @"\w+|""[\w\s]*""");
                        var parameters = argumentParseFailedResult.Command.Parameters;

                        if (!areTooLessArguments && rawArguments.Count < parameters.Count)
                        {
                            reasons.Add(GetText(guildId, Localization.ServiceCommandLessArguments, context.Prefix, argumentParseFailedResult.Command.Name));
                            areTooLessArguments = true;
                        }
                        
                        if (!areTooManyArguments && rawArguments.Count > parameters.Count)
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
                BucketType.Guild => _cooldownService.GenerateKey(context.Command.Name, context.Guild!.Id),
                BucketType.User => _cooldownService.GenerateKey(context.Command.Name, context.User.Id),
                BucketType.Member => _cooldownService.GenerateKey(context.Command.Name, context.Guild!.Id, context.User.Id),
                BucketType.Channel => _cooldownService.GenerateKey(context.Command.Name, context.Channel.Id),
                _ => string.Empty
            };
            
            if (_cooldownService.Has(cooldownKey))
                return;

            _cooldownService.Add(cooldownKey);
            
            retryAfter += TimeSpan.FromSeconds(1);
            await context.Channel.SendErrorMessageAsync(GetText(
                context.Guild?.Id,
                Localization.ServiceCommandCooldown,
                retryAfter.Humanize(culture: new CultureInfo(Localization.GetGuildLocale(context.Guild?.Id)), minUnit: TimeUnit.Second)));
            
            await Task.Delay(retryAfter);
            _cooldownService.Remove(cooldownKey);
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
    }
}