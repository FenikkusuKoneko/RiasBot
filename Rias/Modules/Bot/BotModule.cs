using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
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
        
        [Command("leaveguild")]
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

        [Command("update")]
        [OwnerOnly]
        public async Task UpdateAsync()
        {
            await ReplyConfirmationAsync(Localization.BotUpdate);
            Environment.Exit(69);
        }

        [Command("reload")]
        [OwnerOnly]
        public async Task ReloadAsync(string subcommand)
        {
            switch (subcommand.ToLower())
            {
                case "config":
                case "configuration":
                case "creds":
                case "credentials":
                    Credentials.LoadCredentials();
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
        [OwnerOnly]
        public async Task SendAsync(string id, [Remainder] string message)
        {
            var messageParsed = RiasUtilities.TryParseMessage(message, out var customMessage);
            if (messageParsed && string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null)
            {
                await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                return;
            }
            
            if (id.StartsWith("c:", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!ulong.TryParse(id[2..], out var channelId))
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                var channel = RiasBot.Client.ShardClients
                    .SelectMany(x => x.Value.Guilds)
                    .SelectMany(x => x.Value.Channels)
                    .FirstOrDefault(x => x.Key == channelId).Value;
                
                if (channel is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                if (channel.Type != ChannelType.Text && channel.Type != ChannelType.News && channel.Type != ChannelType.Store)
                {
                    await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                    return;
                }

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

                if (messageParsed)
                    await channel.SendMessageAsync(customMessage.Content, embed: customMessage.Embed);
                else
                    await channel.SendMessageAsync(message);
                
                await ReplyConfirmationAsync(Localization.BotMessageSent);
                return;
            }

            if (id.StartsWith("u:", StringComparison.InvariantCultureIgnoreCase))
            {
                DiscordMember member;
                if (ulong.TryParse(id[2..], out var userId) && RiasBot.Members.TryGetValue(userId, out var m))
                {
                    member = m;
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                    return;
                }
                
                if (member.IsBot)
                {
                    await ReplyErrorAsync(Localization.BotUserIsBot);
                    return;
                }

                try
                {
                    if (messageParsed)
                        await member.SendMessageAsync(customMessage.Content, embed: customMessage.Embed);
                    else
                        await member.SendMessageAsync(message);
                    
                    await ReplyConfirmationAsync(Localization.BotMessageSent);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.BotUserMessageNotSent);
                }
            }
        }

        [Command("edit")]
        [OwnerOnly]
        public async Task EditAsync(string id, [Remainder] string message)
        {
            var ids = id.Split("|");
            if (ids.Length != 2)
            {
                await ReplyErrorAsync(Localization.BotChannelMessageIdsBadFormat);
                return;
            }
            
            if (!ulong.TryParse(ids[0], out var channelId))
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                return;
            }

            var channel = RiasBot.Client.ShardClients
                .SelectMany(x => x.Value.Guilds)
                .SelectMany(x => x.Value.Channels)
                .FirstOrDefault(x => x.Key == channelId).Value;
            
            if (channel is null)
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                return;
            }

            if (channel.Type != ChannelType.Text && channel.Type != ChannelType.News && channel.Type != ChannelType.Store)
            {
                await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                return;
            }
            
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

            DiscordMessage discordMessage;
            if (ulong.TryParse(ids[1], out var messageId))
            {
                discordMessage = await channel.GetMessageAsync(messageId);
            }
            else
            {
                await ReplyErrorAsync(Localization.BotMessageNotFound);
                return;
            }

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

            var messageParsed = RiasUtilities.TryParseMessage(message, out var customMessage);
            if (messageParsed && string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null)
            {
                await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                return;
            }

            if (messageParsed)
                await discordMessage.ModifyAsync(customMessage.Content, customMessage.Embed?.Build());
            else
                await discordMessage.ModifyAsync(message);

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
            
            var embed = new DiscordEmbedBuilder()
                .WithColor(RiasUtilities.ConfirmColor)
                .AddField(GetText(Localization.CommonUser), user.FullName(), true)
                .AddField(GetText(Localization.CommonId), user.Id.ToString(), true)
                .AddField(GetText(Localization.UtilityJoinedDiscord), user.CreationTimestamp.UtcDateTime.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                .AddField(GetText(Localization.BotMutualGuilds), mutualGuilds.ToString(), true)
                .WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto));

            await ReplyAsync(embed);
        }

        [Command("evaluate")]
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