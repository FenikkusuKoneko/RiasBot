using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Extensions
{
    public static class IMessageExtension
    {
        ///<summary>
        ///Send confirmation embed message in current text channel.
        ///</summary>
        public static async Task SendConfirmationEmbed(this IMessageChannel channel, string description)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithDescription(description);
            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        ///<summary>
        ///Send error embed message in current text channel.
        ///</summary>
        public static async Task SendErrorEmbed(this IMessageChannel channel, string description)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
            embed.WithDescription(description);
            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        ///<summary>
        ///Send paginated embed in current text channel.
        ///</summary>
        public static async Task SendPaginated(this IMessageChannel channel, DiscordSocketClient client, string title, string[] list, int itemsPerPage, int currentPage = 0)
        {
            if (currentPage <= 0)
                currentPage = 0;
            else if (currentPage > list.Length % itemsPerPage)
                currentPage = list.Length % itemsPerPage;

            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithTitle(title);

            IEmote arrow_left = new Emoji("⬅");
            IEmote arrow_right = new Emoji("➡");

            IUserMessage msg;

            var lastPage = (list.Length - 1) / itemsPerPage;

            if (list.Length < itemsPerPage)
            {
                embed.WithDescription(String.Join("\n", list, 0, list.Length));
                msg = await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                embed.WithDescription(String.Join("\n", list.Skip(currentPage * itemsPerPage).Take(itemsPerPage)));
                embed.WithFooter(currentPage + "/" + lastPage);
                msg = await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            if (lastPage == 0)
                return;

            await msg.AddReactionAsync(arrow_left).ConfigureAwait(false);
            await msg.AddReactionAsync(arrow_right).ConfigureAwait(false);

            await Task.Delay(2000);

            Action<SocketReaction> changePage = async r =>
            {
                try
                {
                    if (r.Emote.Name == arrow_left.Name)
                    {
                        if (currentPage == 0)
                            return;
                        --currentPage;
                        embed.WithDescription(String.Join("\n", list.Skip(currentPage * itemsPerPage).Take(itemsPerPage)));
                        embed.WithFooter(currentPage + "/" + lastPage);
                        await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
                    }
                    else if (r.Emote.Name == arrow_right.Name)
                    {
                        if (lastPage > currentPage)
                        {
                            ++currentPage;
                            embed.WithDescription(String.Join("\n", list.Skip(currentPage * itemsPerPage).Take(itemsPerPage)));
                            embed.WithFooter(currentPage + "/" + lastPage);
                            await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
                        }
                    }
                }
                catch
                {
                    //ignored
                }
            };

            using (msg.OnReaction(client, changePage, changePage))
            {
                await Task.Delay(30000).ConfigureAwait(false);
            }

            await msg.RemoveAllReactionsAsync().ConfigureAwait(false);
        }
    }
}
