using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Bot
{
    [Name("Bot")]
    public partial class Bot : RiasModule<BotService>
    {
        private readonly DiscordShardedClient _client;

        public Bot(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
        }

        [Command("leaveguild"), OwnerOnly]
        public async Task LeaveGuildAsync(string name)
        {
            var guild = ulong.TryParse(name, out var guildId)
                ? _client.GetGuild(guildId)
                : _client.Guilds.FirstOrDefault(x => string.Equals(x.Name, name));

            if (guild is null)
            {
                await ReplyErrorAsync("GuildNotFound");
                return;
            }

            var embed = new EmbedBuilder()
            {
                Color = RiasUtils.ConfirmColor,
                Description = GetText("LeftGuild", guild.Name)
            }.AddField(GetText("#Common_Id"), guild.Id, true).AddField(GetText("#Common_Users"), guild.MemberCount, true);

            await ReplyAsync(embed);
            await guild.LeaveAsync();
        }

        [Command("update"), OwnerOnly]
        public async Task UpdateAsync()
        {
            await ReplyConfirmationAsync("Update");
            Environment.Exit(0);
        }

        [Command("send"), OwnerOnly]
        public async Task SendAsync(string id, [Remainder] string message)
        {
            var isEmbed = RiasUtils.TryParseEmbed(message, out var embed);
            if (id.StartsWith("c:", StringComparison.InvariantCultureIgnoreCase))
            {
                SocketChannel channel;
                if (ulong.TryParse(id[2..], out var channelId))
                {
                    channel = _client.GetChannel(channelId);
                }
                else
                {
                    await ReplyErrorAsync("#Administration_TextChannelNotFound");
                    return;
                }

                if (channel is null)
                {
                    await ReplyErrorAsync("#Administration_TextChannelNotFound");
                    return;
                }

                if (!(channel is SocketTextChannel textChannel))
                {
                    await ReplyErrorAsync("ChannelNotTextChannel");
                    return;
                }

                var permissions = Context.CurrentGuildUser!.GetPermissions(textChannel);
                if (!permissions.ViewChannel)
                {
                    await ReplyErrorAsync("#Administration_TextChannelNoViewPermission");
                    return;
                }

                if (!permissions.SendMessages)
                {
                    await ReplyErrorAsync("#Administration_TextChannelNoSendMessagesPermission");
                    return;
                }

                if (isEmbed)
                    await textChannel.SendMessageAsync(embed);
                else
                    await textChannel.SendMessageAsync(message);
                
                await ReplyConfirmationAsync("MessageSent");
                return;
            }

            if (id.StartsWith("u:", StringComparison.InvariantCultureIgnoreCase))
            {
                SocketUser user;
                if (ulong.TryParse(id[2..], out var userId))
                {
                    user = _client.GetUser(userId);
                }
                else
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }

                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }
                
                if (user.IsBot)
                {
                    await ReplyErrorAsync("UserIsBot");
                    return;
                }

                try
                {
                    if (isEmbed)
                        await user.SendMessageAsync(embed);
                    else
                        await user.SendMessageAsync(message);
                    
                    await ReplyConfirmationAsync("MessageSent");
                }
                catch
                {
                    await ReplyErrorAsync("UserMessageNotSent");
                }
            }
        }

        [Command("edit"), OwnerOnly]
        public async Task EditAsync(string id, [Remainder] string message)
        {
            var ids = id.Split("|");
            if (ids.Length != 2)
            {
                await ReplyErrorAsync("ChannelMessageIdsBadFormat");
                return;
            }

            SocketChannel channel;
            if (ulong.TryParse(ids[0], out var channelId))
            {
                channel = _client.GetChannel(channelId);
            }
            else
            {
                await ReplyErrorAsync("#Administration_TextChannelNotFound");
                return;
            }

            if (channel is null)
            {
                await ReplyErrorAsync("#Administration_TextChannelNotFound");
                return;
            }
            
            if (!(channel is SocketTextChannel textChannel))
            {
                await ReplyErrorAsync("ChannelNotTextChannel");
                return;
            }
            
            var permissions = Context.CurrentGuildUser!.GetPermissions(textChannel);
            if (!permissions.ViewChannel)
            {
                await ReplyErrorAsync("#Administration_TextChannelNoViewPermission");
                return;
            }

            if (!permissions.SendMessages)
            {
                await ReplyErrorAsync("#Administration_TextChannelNoSendMessagesPermission");
                return;
            }

            IMessage restMessage;
            if (ulong.TryParse(ids[1], out var messageId))
            {
                restMessage = await textChannel.GetMessageAsync(messageId);
            }
            else
            {
                await ReplyErrorAsync("MessageNotFound");
                return;
            }

            if (restMessage is null)
            {
                await ReplyErrorAsync("MessageNotFound");
                return;
            }

            if (!(restMessage is RestUserMessage restUserMessage))
            {
                await ReplyErrorAsync("MessageNotUserMessage");
                return;
            }

            if (restUserMessage.Author.Id != Context.CurrentGuildUser.Id)
            {
                await ReplyErrorAsync("MessageNotSelf");
                return;
            }

            if (RiasUtils.TryParseEmbed(message, out var embed))
                await restUserMessage.ModifyAsync(x =>
                {
                    x.Content = null;
                    x.Embed = embed.Build();
                });
            else
                await restUserMessage.ModifyAsync(x =>
                {
                    x.Content = message;
                    x.Embed = null;
                });

            await ReplyConfirmationAsync("MessageEdited");
        }

        [Command("finduser"), OwnerOnly]
        public async Task FindUserAsync([Remainder] string value)
        {
            IUser? user = null;
            if (ulong.TryParse(value, out var userId))
            {
                user = _client.GetUser(userId) ?? (IUser) await Context.Client.Rest.GetUserAsync(userId);
            }
            else
            {
                var index = value.LastIndexOf("#", StringComparison.Ordinal);
                if (index >= 0)
                {
                    user = _client.GetUser(value[..index], value[(index + 1)..]);
                }
            }
            
            if (user is null)
            {
                await ReplyErrorAsync("#Administration_UserNotFound");
                return;
            }
            
            var mutualGuilds = 0;
            if (user is SocketGuildUser guildUser)
                mutualGuilds = guildUser.MutualGuilds.Count;

            var embed = new EmbedBuilder()
            .WithColor(RiasUtils.ConfirmColor)
            .AddField(GetText("#Common_User"), user, true)
            .AddField(GetText("#Common_Id"), user.Id, true)
            .AddField(GetText("#Utility_JoinedDiscord"), user.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
            .AddField(GetText("MutualGuilds"), mutualGuilds, true)
            .WithImageUrl(user.GetRealAvatarUrl());

            await ReplyAsync(embed);
        }

        [Command("evaluate"), OwnerOnly]
        public async Task EvaluateAsync([Remainder] string code)
        {
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Author = new EmbedAuthorBuilder
                {
                    Name = GetText("RoslynCompiler"),
                    IconUrl = Context.User.GetRealAvatarUrl()
                },
                Description = GetText("EvaluatingCode"),
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
            
            embed.AddField(GetText("Code"), Format.Code(evaluation.Code, "csharp"));
            if (evaluation.Success)
            {
                embed.WithDescription(GetText("CodeEvaluated"));
                embed.AddField(evaluation.ReturnType, Format.Code(evaluation.Result, "csharp"));
                embed.AddField(GetText("CompilationTime"), $"{evaluation.CompilationTime?.TotalMilliseconds} ms", true);
                embed.AddField(GetText("ExecutionTime"), $"{evaluation.ExecutionTime?.TotalMilliseconds} ms", true);
            }
            else if (evaluation.IsCompiled)
            {
                embed.WithDescription(GetText("CodeCompiledWithError"));
                embed.AddField(GetText("Exception"), Format.Code(evaluation.Exception));
                embed.AddField(GetText("CompilationTime"), $"{evaluation.CompilationTime?.TotalMilliseconds} ms", true);
                embed.AddField(GetText("ExecutionTime"), $"{evaluation.ExecutionTime?.TotalMilliseconds} ms", true);
            }
            else
            {
                embed.WithDescription(GetText("CodeEvaluatedWithError"));
                embed.AddField(GetText("CompilationTime"), Format.Code(evaluation.Exception));
                embed.AddField("Compilation time", $"{evaluation.CompilationTime?.TotalMilliseconds} ms", true);
            }
            
            await message.ModifyAsync(m => m.Embed = embed.Build());
        }

        [Command("downloadusers"), OwnerOnly]
        public async Task DownloadUsersAsync(ulong? guildId = null)
        {
            var guild = guildId.HasValue ? _client.GetGuild(guildId.Value) : Context.Guild;
            if (guild is null)
            {
                await ReplyErrorAsync("GuildNotFound");
                return;
            }
            
            await guild.DownloadUsersAsync();
            await ReplyConfirmationAsync("GuildUsersDownloaded", guild.Name);
        }
    }
}