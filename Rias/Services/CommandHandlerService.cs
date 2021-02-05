using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
        public readonly CommandStatistics CommandStatistics = new();
        
        private readonly CommandService _commandService;
        private readonly BotService _botService;
        private readonly XpService _xpService;
        
        private readonly ConcurrentHashSet<string> _cooldowns = new();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _commandsInfo = new();
        private readonly string _commandsPath = Path.Combine(Environment.CurrentDirectory, "assets/commands");

        private List<Type> _typeParsers = new();

        public CommandHandlerService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _commandService = serviceProvider.GetRequiredService<CommandService>();
            _botService = serviceProvider.GetRequiredService<BotService>();
            _xpService = serviceProvider.GetRequiredService<XpService>();
            
            RiasBot.Client.MessageCreated += MessageCreatedAsync;
            
            var sw = Stopwatch.StartNew();
            
            var assembly = Assembly.GetAssembly(typeof(RiasBot));
            _commandService.AddModules(assembly);
            
#if !DEBUG
            var testModule = _commandService.GetAllModules().FirstOrDefault(x => string.Equals(x.Name, "Test", StringComparison.OrdinalIgnoreCase));
            if (testModule is not null)
                _commandService.RemoveModule(testModule);
#endif
            
            LoadCommands();
            LoadTypeParsers();
            sw.Stop();


            Log.Information($"Commands loaded: {sw.ElapsedMilliseconds} ms");
        }

        public void ReloadCommands()
        {
            _commandsInfo.Clear();
            LoadCommands();
        }

        private void LoadCommands()
        {
            foreach (var commandsInfo in Directory.GetFiles(_commandsPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(commandsInfo);
                _commandsInfo.TryAdd(fileName, JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(commandsInfo))!);
            }
        }

        public string GetCommandInfo(ulong? guildId, string key, params object[] args)
        {
            var locale = guildId.HasValue ? Localization.GetGuildLocale(guildId.Value) : Localization.DefaultLocale;
            if (TryGetCommandInfoString(locale, key, out var @string) && !string.IsNullOrEmpty(@string))
                return args.Length == 0 ? @string : string.Format(@string, args);

            if (!string.Equals(locale, Localization.DefaultLocale)
                && TryGetCommandInfoString(Localization.DefaultLocale, key, out @string)
                && !string.IsNullOrEmpty(@string))
                return args.Length == 0 ? @string : string.Format(@string, args);

            throw new InvalidOperationException($"The command info for the command \"{key}\" couldn't be found.");
        }
        
        private bool TryGetCommandInfoString(string locale, string key, out string? value)
        {
            if (_commandsInfo.TryGetValue(locale, out var localeDictionary))
            {
                if (localeDictionary.TryGetValue(key, out var @string))
                {
                    value = @string;
                    return true;
                }
            }

            value = null;
            return false;
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
                var member = (DiscordMember) args.Author;
                RiasBot.Members[member.Id] = member;
                
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

            if (await CheckUserBan(args.Author) && args.Author.Id != Configuration.MasterId)
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
                    await RunTaskAsync(SendFailedResultsAsync(context, new[] { (FailedResult) result }));
                    break;
                case CommandOnCooldownResult commandOnCooldownResult:
                    await RunTaskAsync(SendCommandOnCooldownMessageAsync(context, commandOnCooldownResult));
                    break;
            }
        }

        private Task SendFailedResultsAsync(RiasCommandContext context, IEnumerable<FailedResult> failedResults)
        {
            var guildId = context.Guild?.Id;
            var reasons = new HashSet<string>();
            var argumentsFailed = false;
            
            foreach (var failedResult in failedResults)
            {
                switch (failedResult)
                {
                    case ChecksFailedResult checksFailedResult:
                        foreach (var (_, result) in checksFailedResult.FailedChecks)
                            reasons.Add(result.Reason);
                        
                        break;
                    case TypeParseFailedResult typeParseFailedResult:
                        reasons.Add(_typeParsers.Any(x => x.BaseType!.GetGenericArguments()[0] == typeParseFailedResult.Parameter.Type)
                            ? typeParseFailedResult.Reason
                            : GetText(guildId, Localization.TypeParserPrimitiveType, context.Prefix, typeParseFailedResult.Parameter.Command.Name));

                        break;
                    case ArgumentParseFailedResult:
                        argumentsFailed = true;
                        break;
                }
            }

            if (reasons.Count == 0)
            {
                return argumentsFailed
                    ? context.Channel.SendMessageAsync(GenerateHelpEmbedAsync(context.Guild, context.Command, context.Prefix))
                    : Task.CompletedTask;
            }

            if (argumentsFailed)
                reasons.Add(GetText(context.Guild?.Id, Localization.HelpBadArguments));
            
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ErrorColor,
                Title = GetText(guildId, Localization.ServiceCommandNotExecuted),
                Description = GetText(context.Guild?.Id, Localization.HelpCommandInformation, context.Prefix, context.Command.Name)
            }.AddField(GetText(context.Guild?.Id, reasons.Count == 1 ? Localization.CommonReason : Localization.CommonReasons), string.Join("\n", reasons.Select(x => $"• {x}")));

            return context.Channel.SendMessageAsync(embed);
        }
        
        private async Task SendCommandOnCooldownMessageAsync(RiasCommandContext context, CommandOnCooldownResult result)
        {
            var (cooldown, retryAfter) = result.Cooldowns[0];
            var cooldownKey = (BucketType) cooldown.BucketType switch
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
                return Configuration.Prefix;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var prefix = (await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id))?.Prefix;
            
            return !string.IsNullOrEmpty(prefix) ? prefix : Configuration.Prefix;
        }
        
        public DiscordEmbedBuilder GenerateHelpEmbedAsync(DiscordGuild? guild, Command command, string prefix)
        {
            var moduleName = command.Module.Name;
            if (command.Module.Parent != null && !string.Equals(command.Module.Name, command.Module.Parent.Name, StringComparison.OrdinalIgnoreCase))
                moduleName = $"{command.Module.Parent.Name} -> {moduleName}";

            var moduleAlias = command.Module.Aliases.Count != 0 ? $"{command.Module.Aliases[0]} " : string.Empty;
            var title = string.Join(" / ", command.Aliases.Select(a => $"{prefix}{moduleAlias}{a}"));
            
            if (string.IsNullOrEmpty(title))
                title = $"{prefix}{moduleAlias}";

            if (command.Checks.Any(c => c is OwnerOnlyAttribute))
                title += $" [{GetText(guild?.Id, Localization.HelpOwnerOnly).ToLowerInvariant()}]";

            var commandInfoKey = command.Aliases.Count != 0
                ? $"{command.Module.Name.Replace(" ", "_").ToLower()}_{command.Aliases[0]}"
                : command.Name;
            
            var description = GetCommandInfo(guild?.Id, commandInfoKey);

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = GetText(guild?.Id, Localization.HelpModule, moduleName)
                },
                Title = title,
                Description = description.Replace("[prefix]", prefix).Replace("[currency]", Configuration.Currency)
            };

            var permissions = new StringBuilder();

            foreach (var attribute in command.Checks)
            {
                switch (attribute)
                {
                    case MemberPermissionAttribute memberPermissionAttribute:
                        var memberPermissions = memberPermissionAttribute.Permissions
                            .GetValueOrDefault()
                            .ToString()
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => Formatter.InlineCode(x.Humanize(LetterCasing.Title)))
                            .ToArray();
                        permissions.Append(GetText(guild?.Id, Localization.HelpRequiresMemberPermissions, string.Join(", ", memberPermissions)));
                        break;
                    case BotPermissionAttribute botPermissionAttribute:
                        var botPermissions = botPermissionAttribute.Permissions
                            .GetValueOrDefault()
                            .ToString()
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => Formatter.InlineCode(x.Humanize(LetterCasing.Title)))
                            .ToArray();
                        
                        if (permissions.Length > 0)
                            permissions.AppendLine();
                        
                        permissions.Append(GetText(guild?.Id, Localization.HelpRequiresBotPermissions,  RiasBot.Client.CurrentUser.Username, string.Join(", ", botPermissions)));
                        break;
                    case OwnerOnlyAttribute:
                        title = $"{title} **({GetText(guild?.Id, Localization.HelpOwnerOnly)})**";
                        break;
                }
            }

            if (permissions.Length != 0)
                embed.AddField(GetText(guild?.Id, Localization.HelpRequiresPermissions), permissions.ToString());
            
            var commandExamples = GetCommandInfo(guild?.Id, $"{commandInfoKey}_examples").Split('\n');
            var usages = new StringBuilder();
            var examples = new StringBuilder();

            foreach (var commandExample in commandExamples)
            {
                if (commandExample[0] == '[' && commandExample[^1] == ']')
                    usages.AppendLine(commandExample[1..^1]);
                else
                    examples.AppendLine(commandExample);
            }
            
            embed.AddField(GetText(guild?.Id, Localization.CommonExamples), Formatter.InlineCode(string.Format(examples.ToString(), prefix)), true);
            embed.AddField(GetText(guild?.Id, Localization.CommonUsages), Formatter.InlineCode(string.Format(usages.ToString(), prefix)), true);
            
            var commandCooldown = command.Cooldowns.FirstOrDefault();
            if (commandCooldown != null)
            {
                var locale = Localization.GetGuildLocale(guild?.Id);
                embed.AddField(GetText(guild?.Id, Localization.CommonCooldown),
                    $"{GetText(guild?.Id, Localization.CommonAmount)}: **{commandCooldown.Amount}**\n" +
                    $"{GetText(guild?.Id, Localization.CommonPeriod)}: **{commandCooldown.Per.Humanize(culture: new CultureInfo(locale))}**\n" +
                    $"{GetText(guild?.Id, Localization.CommonPer)}: **{GetText(guild?.Id, Localization.CommonCooldownBucketType(commandCooldown.BucketType.Humanize(LetterCasing.LowerCase).Underscore()))}**");
            }
            
            embed.WithCurrentTimestamp();
            return embed;
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
    }
}