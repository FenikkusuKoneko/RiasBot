using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Bot
{
    [Name("Bot")]
    public partial class BotModule : RiasModule<BotService>
    {
        public BotModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        [Command("leaveguild"), OwnerOnly]
        public async Task LeaveGuildAsync(string name)
        {
            var guild = ulong.TryParse(name, out var guildId)
                ? RiasBot.GetGuild(guildId)
                : RiasBot.Guilds.FirstOrDefault(x => string.Equals(x.Value.Name, name)).Value;

            if (guild is null)
            {
                await ReplyErrorAsync(Localization.BotGuildNotFound);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Description = GetText(Localization.BotLeftGuild, guild.Name)
            }.AddField(GetText(Localization.CommonId), guild.Id.ToString(), true).AddField(GetText(Localization.CommonUsers), guild.MemberCount.ToString(), true);

            await ReplyAsync(embed);
            await guild.LeaveAsync();
        }
        
        [Command("shutdown"), OwnerOnly]
        public async Task ShutdownAsync()
        {
            await ReplyConfirmationAsync(Localization.BotShutdown);
            Environment.Exit(0);
        }
        
        [Command("update"), OwnerOnly]
        public async Task UpdateAsync()
        {
            await ReplyConfirmationAsync(Localization.BotUpdate);
            Environment.Exit(69);
        }
        
        [Command("send"), OwnerOnly]
        public async Task SendAsync(string id, [Remainder] string message)
        {
            var isEmbed = RiasUtilities.TryParseEmbed(message, out var embed);
            if (id.StartsWith("c:", StringComparison.InvariantCultureIgnoreCase))
            {
                DiscordChannel channel;
                if (ulong.TryParse(id[2..], out var channelId) && RiasBot.Channels.TryGetValue(channelId, out var c))
                    channel = c;
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                if (channel.Type != ChannelType.Text)
                {
                    await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                    return;
                }

                var permissions = Context.CurrentMember!.PermissionsIn(channel);
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

                if (isEmbed)
                    await channel.SendMessageAsync(embed);
                else
                    await channel.SendMessageAsync(message);
                
                await ReplyConfirmationAsync(Localization.BotMessageSent);
                return;
            }

            if (id.StartsWith("u:", StringComparison.InvariantCultureIgnoreCase))
            {
                DiscordMember member;
                if (ulong.TryParse(id[2..], out var userId) && RiasBot.Members.TryGetValue(userId, out var m))
                    member = m;
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
                    if (isEmbed)
                        await member.SendMessageAsync(embed: embed);
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
        
        [Command("edit"), OwnerOnly]
        public async Task EditAsync(string id, [Remainder] string message)
        {
            var ids = id.Split("|");
            if (ids.Length != 2)
            {
                await ReplyErrorAsync(Localization.BotChannelMessageIdsBadFormat);
                return;
            }

            DiscordChannel channel;
            if (ulong.TryParse(ids[0], out var channelId) && RiasBot.Channels.TryGetValue(channelId, out var c))
                channel = c;
            else
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                return;
            }

            if (channel.Type != ChannelType.Text)
            {
                await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                return;
            }
            
            var permissions = Context.CurrentMember!.PermissionsIn(channel);
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
                discordMessage = await channel.GetMessageAsync(messageId);
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

            if (discordMessage.Author.Id != Context.CurrentMember.Id)
            {
                await ReplyErrorAsync(Localization.BotMessageNotSelf);
                return;
            }

            if (RiasUtilities.TryParseEmbed(message, out var embed))
                await discordMessage.ModifyAsync(embed: embed.Build());
            else
                await discordMessage.ModifyAsync(message);

            await ReplyConfirmationAsync(Localization.BotMessageEdited);
        }
        
        [Command("finduser"), OwnerOnly]
        public async Task FindUserAsync([Remainder] string value)
        {
            DiscordUser? user = null;
            if (ulong.TryParse(value, out var userId))
            {
                user = RiasBot.Members.TryGetValue(userId, out var u)
                    ? u
                    : await RiasBot.Client.ShardClients[0].GetUserAsync(userId);
            }
            else
            {
                var index = value.LastIndexOf("#", StringComparison.Ordinal);
                if (index >= 0)
                    user = RiasBot.Members.FirstOrDefault(x => string.Equals(x.Value.Username, value[..index])
                                                               && string.Equals(x.Value.Discriminator, value[(index + 1)..])).Value;
            }
            
            if (user is null)
            {
                await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                return;
            }
            
            var mutualGuilds = user is DiscordMember member ? member.GetMutualGuilds(RiasBot).Count : 0;
            
            var embed = new DiscordEmbedBuilder()
                .WithColor(RiasUtilities.ConfirmColor)
                .AddField(GetText(Localization.CommonUser), user.FullName(), true)
                .AddField(GetText(Localization.CommonId), user.Id.ToString(), true)
                .AddField(GetText(Localization.UtilityJoinedDiscord), user.CreationTimestamp.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                .AddField(GetText(Localization.BotMutualGuilds), mutualGuilds.ToString(), true)
                .WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto));

            await ReplyAsync(embed);
        }

        [Command("evaluate"), OwnerOnly]
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
            
            if (evaluation is null)
            {
                await message.DeleteAsync();
                return;
            }
            
            embed.AddField(GetText(Localization.BotCode), $"```csharp\n{evaluation.Code}\n```");
            if (evaluation.Success)
            {
                embed.WithDescription(GetText(Localization.BotCodeEvaluated));
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
        }
    }
}