using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
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

            var embed = new LocalEmbedBuilder()
            {
                Color = RiasUtilities.ConfirmColor,
                Description = GetText(Localization.BotLeftGuild, guild.Name)
            }.AddField(GetText(Localization.CommonId), guild.Id, true).AddField(GetText(Localization.CommonUsers), guild.MemberCount, true);

            await ReplyAsync(embed);
            await guild.LeaveAsync();
        }
        
        [Command("update"), OwnerOnly]
        public async Task UpdateAsync()
        {
            //TODO Create a manager for installing/updating and running RiasBot
            await ReplyConfirmationAsync(Localization.BotUpdate);
            Environment.Exit(0);
        }
        
        [Command("send"), OwnerOnly]
        public async Task SendAsync(string id, [Remainder] string message)
        {
            var isEmbed = RiasUtilities.TryParseEmbed(message, out var embed);
            if (id.StartsWith("c:", StringComparison.InvariantCultureIgnoreCase))
            {
                CachedChannel channel;
                if (ulong.TryParse(id[2..], out var channelId))
                {
                    channel = RiasBot.GetChannel(channelId);
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                if (channel is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                if (!(channel is CachedTextChannel textChannel))
                {
                    await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                    return;
                }

                var permissions = Context.CurrentMember!.GetPermissionsFor(textChannel);
                if (!permissions.ViewChannel)
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                    return;
                }

                if (!permissions.SendMessages)
                {
                    await ReplyErrorAsync(Localization.BotTextChannelNoSendMessagesPermission);
                    return;
                }

                if (isEmbed)
                    await textChannel.SendMessageAsync(embed);
                else
                    await textChannel.SendMessageAsync(message);
                
                await ReplyConfirmationAsync(Localization.BotMessageSent);
                return;
            }

            if (id.StartsWith("u:", StringComparison.InvariantCultureIgnoreCase))
            {
                CachedUser user;
                if (ulong.TryParse(id[2..], out var userId))
                {
                    user = RiasBot.GetUser(userId);
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                    return;
                }

                if (user is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                    return;
                }
                
                if (user.IsBot)
                {
                    await ReplyErrorAsync(Localization.BotUserIsBot);
                    return;
                }

                try
                {
                    if (isEmbed)
                        await user.SendMessageAsync(embed: embed.Build());
                    else
                        await user.SendMessageAsync(message);
                    
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

            CachedChannel channel;
            if (ulong.TryParse(ids[0], out var channelId))
            {
                channel = RiasBot.GetChannel(channelId);
            }
            else
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                return;
            }

            if (channel is null)
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                return;
            }
            
            if (!(channel is CachedTextChannel textChannel))
            {
                await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                return;
            }
            
            var permissions = Context.CurrentMember!.GetPermissionsFor(textChannel);
            if (!permissions.ViewChannel)
            {
                await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                return;
            }

            if (!permissions.SendMessages)
            {
                await ReplyErrorAsync(Localization.BotTextChannelNoSendMessagesPermission);
                return;
            }

            IMessage restMessage;
            if (ulong.TryParse(ids[1], out var messageId))
            {
                restMessage = await textChannel.GetMessageAsync(messageId);
            }
            else
            {
                await ReplyErrorAsync(Localization.BotMessageNotFound);
                return;
            }

            if (restMessage is null)
            {
                await ReplyErrorAsync(Localization.BotMessageNotFound);
                return;
            }

            if (!(restMessage is RestUserMessage restUserMessage))
            {
                await ReplyErrorAsync(Localization.BotMessageNotUserMessage);
                return;
            }

            if (restUserMessage.Author.Id != Context.CurrentMember.Id)
            {
                await ReplyErrorAsync(Localization.BotMessageNotSelf);
                return;
            }

            if (RiasUtilities.TryParseEmbed(message, out var embed))
            {
                await restUserMessage.ModifyAsync(x =>
                {
                    x.Content = null;
                    x.Embed = embed.Build();
                });
            }
            else
            {
                await restUserMessage.ModifyAsync(x =>
                {
                    x.Content = message;
                    x.Embed = null;
                });
            }

            await ReplyConfirmationAsync(Localization.BotMessageEdited);
        }
        
        [Command("finduser"), OwnerOnly]
        public async Task FindUserAsync([Remainder] string value)
        {
            IUser? user = null;
            if (ulong.TryParse(value, out var userId))
            {
                user = RiasBot.GetUser(userId) ?? (IUser) await RiasBot.GetUserAsync(userId);
            }
            else
            {
                var index = value.LastIndexOf("#", StringComparison.Ordinal);
                if (index >= 0)
                {
                    user = RiasBot.Users.FirstOrDefault(x => string.Equals(x.Value.Name, value[..index])
                                                             && string.Equals(x.Value.Discriminator, value[(index + 1)..])).Value;
                }
            }
            
            if (user is null)
            {
                await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                return;
            }

            var mutualGuilds = user is CachedUser cachedUser ? cachedUser.MutualGuilds.Count : 0;

            var embed = new LocalEmbedBuilder()
                .WithColor(RiasUtilities.ConfirmColor)
                .AddField(GetText(Localization.CommonUser), user, true)
                .AddField(GetText(Localization.CommonId), user.Id, true)
                .AddField(GetText(Localization.UtilityJoinedDiscord), user.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                .AddField(GetText(Localization.BotMutualGuilds), mutualGuilds, true)
                .WithImageUrl(user.GetAvatarUrl());

            await ReplyAsync(embed);
        }

        [Command("evaluate"), OwnerOnly]
        public async Task EvaluateAsync([Remainder] string code)
        {
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Author = new LocalEmbedAuthorBuilder
                {
                    Name = GetText(Localization.BotRoslynCompiler),
                    IconUrl = Context.User.GetAvatarUrl()
                },
                Description = GetText(Localization.BotEvaluatingCode),
                Timestamp = DateTimeOffset.UtcNow
            };
            
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
            
            await message.ModifyAsync(m => m.Embed = embed.Build());
        }
    }
}