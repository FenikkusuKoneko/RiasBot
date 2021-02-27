using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Bot
{
    [Name("Bot")]
    public partial class BotModule : RiasModule<BotService>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandHandlerService _commandHandlerService;
        private readonly UnitsService _unitsService;
        
        public BotModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _commandHandlerService = serviceProvider.GetRequiredService<CommandHandlerService>();
            _unitsService = serviceProvider.GetRequiredService<UnitsService>();
        }
        
        [Command("leaveguild", "leaveserver")]
        [OwnerOnly]
        public async Task LeaveGuildAsync(string name)
        {
            var guild = ulong.TryParse(name, out var guildId)
                ? RiasBot.GetGuild(guildId)
                : RiasBot.Client.ShardClients
                    .SelectMany(x => x.Value.Guilds)
                    .FirstOrDefault(x => string.Equals(x.Value.Name, name)).Value;

            if (guild is null)
            {
                await ReplyErrorAsync(Localization.BotGuildNotFound);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Description = GetText(Localization.BotLeftGuild, guild.Name)
            }.AddField(GetText(Localization.CommonId), guild.Id.ToString(), true).AddField(GetText(Localization.CommonMembers), guild.MemberCount.ToString(), true);

            await ReplyAsync(embed);
            await guild.LeaveAsync();
        }

        [Command("shutdown")]
        [OwnerOnly]
        public async Task ShutdownAsync()
        {
            await ReplyConfirmationAsync(Localization.BotShutdown);
            Environment.Exit(0);
        }

        [Command("update", "restart")]
        [OwnerOnly]
        public async Task UpdateAsync()
        {
            await ReplyConfirmationAsync(Localization.BotUpdate);
            Environment.Exit(69);
        }

        [Command("reload", "refresh")]
        [OwnerOnly]
        public async Task ReloadAsync(string subcommand)
        {
            switch (subcommand.ToLower())
            {
                case "config":
                case "configuration":
                case "creds":
                case "credentials":
                    Configuration.LoadCredentials();
                    await ReplyConfirmationAsync(Localization.BotCredentialsReloaded);
                    break;
                case "commands":
                    _commandHandlerService.ReloadCommands();
                    await ReplyConfirmationAsync(Localization.BotCommandsReloaded);
                    break;
                case "locales":
                case "translations":
                    Localization.Reload(_serviceProvider);
                    await ReplyConfirmationAsync(Localization.BotLocalesReloaded);
                    break;
                case "units":
                    _unitsService.ReloadUnits();
                    await ReplyConfirmationAsync(Localization.BotUnitsReloaded);
                    break;
            }
        }

        [Command("send")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task SendAsync([TextChannel] DiscordChannel channel, [Remainder] string message)
        {
            var permissions = channel.Guild.CurrentMember.PermissionsIn(channel);
            if (!permissions.HasPermission(Permissions.AccessChannels))
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                return;
            }

            if (!permissions.HasPermission(Permissions.SendMessages))
            {
                await ReplyErrorAsync(Localization.BotTextChannelNoSendMessagesPermission);
                return;
            }
            
            switch (RiasUtilities.TryParseMessage(message, out var customMessage))
            {
                case true when string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null:
                    await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                    return;
                case true:
                    await channel.SendMessageAsync(customMessage.Content, customMessage.Embed);
                    break;
                default:
                    await channel.SendMessageAsync(message);
                    break;
            }

            await ReplyConfirmationAsync(Localization.BotMessageSent);
        }

        [Command("edit")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task EditAsync([TextChannel] DiscordChannel channel, ulong messageId, [Remainder] string message)
        {
            var permissions = channel.Guild.CurrentMember.PermissionsIn(channel);
            if (!permissions.HasPermission(Permissions.AccessChannels))
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                return;
            }

            if (!permissions.HasPermission(Permissions.SendMessages))
            {
                await ReplyErrorAsync(Localization.BotTextChannelNoSendMessagesPermission);
                return;
            }

            var discordMessage = await channel.GetMessageAsync(messageId);
            if (discordMessage is null)
            {
                await ReplyErrorAsync(Localization.BotMessageNotFound);
                return;
            }

            if (discordMessage.MessageType != MessageType.Default)
            {
                await ReplyErrorAsync(Localization.BotMessageNotUserMessage);
                return;
            }

            if (discordMessage.Author.Id != channel.Guild.CurrentMember.Id)
            {
                await ReplyErrorAsync(Localization.BotMessageNotSelf);
                return;
            }

            switch (RiasUtilities.TryParseMessage(message, out var customMessage))
            {
                case true when string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null:
                    await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                    return;
                case true:
                    await discordMessage.ModifyAsync(customMessage.Content, customMessage.Embed?.Build());
                    break;
                default:
                    await discordMessage.ModifyAsync(message, null);
                    break;
            }

            await ReplyConfirmationAsync(Localization.BotMessageEdited);
        }

        [Command("finduser")]
        [OwnerOnly]
        public async Task FindUserAsync([Remainder] string value)
        {
            DiscordUser? user = null;
            if (ulong.TryParse(value, out var userId))
            {
                user = await RiasBot.GetUserAsync(userId);
                if (user is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                    return;
                }
            }
            else
            {
                var index = value.LastIndexOf("#", StringComparison.Ordinal);
                if (index >= 0)
                {
                    user = RiasBot.Members.FirstOrDefault(x => string.Equals(x.Value.Username, value[..index])
                                                               && string.Equals(x.Value.Discriminator, value[(index + 1)..])).Value;
                }
            }
            
            if (user is null)
            {
                await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                return;
            }
            
            var mutualGuilds = user is DiscordMember member ? member.GetMutualGuilds(RiasBot).Count() : 0;
            
            var locale = Localization.GetGuildLocale(Context.Guild!.Id);
            var creationTimestampDateTime = user.CreationTimestamp.UtcDateTime;
            var creationTimestamp = $"{creationTimestampDateTime:yyyy-MM-dd HH:mm:ss}\n" +
                                    $"`{GetText(Localization.UtilityDateTimeAgo, (DateTime.UtcNow - creationTimestampDateTime).Humanize(6, new CultureInfo(locale), TimeUnit.Year, TimeUnit.Second))}`";
            
            var embed = new DiscordEmbedBuilder()
                .WithColor(RiasUtilities.ConfirmColor)
                .AddField(GetText(Localization.CommonUser), user.FullName(), true)
                .AddField(GetText(Localization.CommonId), user.Id.ToString(), true)
                .AddField(GetText(Localization.UtilityJoinedDiscord), creationTimestamp, true)
                .AddField(GetText(Localization.BotMutualGuilds), mutualGuilds.ToString(), true)
                .WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto));

            await ReplyAsync(embed);
        }

        [Command("evaluate", "eval")]
        [OwnerOnly]
        public async Task EvaluateAsync([Remainder] string code)
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Description = GetText(Localization.BotEvaluatingCode),
                Timestamp = DateTimeOffset.UtcNow
            }.WithAuthor(GetText(Localization.BotRoslynCompiler), Context.User.GetAvatarUrl(ImageFormat.Auto));
            
            var message = await ReplyAsync(embed);
            var evaluation = await Service.EvaluateAsync(Context, code);
            GC.Collect();

            if (evaluation.Success)
            {
                embed.WithDescription(GetText(Localization.BotCodeEvaluated));
                
                if (evaluation.ReturnType is null) 
                    embed.AddField("Null", "null");
                else
                    embed.AddField(evaluation.ReturnType, $"```csharp\n{evaluation.Result}\n```");
                
                embed.AddField(GetText(Localization.BotCompilationTime), $"{evaluation.CompilationTime?.TotalMilliseconds:F2} ms", true);
                embed.AddField(GetText(Localization.BotExecutionTime), $"{evaluation.ExecutionTime?.TotalMilliseconds:F2} ms", true);
            }
            else if (evaluation.IsCompiled)
            {
                var exception = GetText(Localization.BotError);
                if (!string.IsNullOrEmpty(evaluation.ReturnType))
                    exception += $" ({evaluation.ReturnType})";
                
                embed.WithDescription(GetText(Localization.BotCodeCompiledWithError));
                embed.AddField(exception, $"```\n{evaluation.Exception}\n```");
                embed.AddField(GetText(Localization.BotCompilationTime), $"{evaluation.CompilationTime?.TotalMilliseconds:F2} ms", true);
                embed.AddField(GetText(Localization.BotExecutionTime), $"{evaluation.ExecutionTime?.TotalMilliseconds:F2} ms", true);
            }
            else
            {
                embed.WithDescription(GetText(Localization.BotCodeEvaluatedWithError));
                embed.AddField(GetText(Localization.BotError), $"```\n{evaluation.Exception}\n```");
                embed.AddField(GetText(Localization.BotCompilationTime), $"{evaluation.CompilationTime?.TotalMilliseconds:F2} ms", true);
            }

            await message.ModifyAsync(embed: embed.Build());

            if (evaluation.Success && evaluation.ReturnType is null)
            {
                var xEmoji = DiscordEmoji.FromUnicode("❌");
                await message.CreateReactionAsync(xEmoji);
                
                var reactionResult = await Context.Interactivity.WaitForReactionAsync(x => x.Emoji.Equals(xEmoji), message, Context.User);
                if (!reactionResult.TimedOut)
                    await message.DeleteAsync();
            }
        }
    }
}