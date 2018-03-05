using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ImageSharp;
using Newtonsoft.Json;
using RiasBot.Commons;
using System;
using System.Threading.Tasks;

namespace RiasBot.Extensions
{
    public static class Extensions
    {
        public static ModuleInfo GetModule(this ModuleInfo module)
        {
            if (module.Parent != null)
            {
                module = module.Parent;
            }
            return module;
        }

        public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client, Action<SocketReaction> reactionAdded, Action<SocketReaction> reactionRemoved = null)
        {
            if (reactionRemoved == null)
                reactionRemoved = delegate { };

            var wrap = new ReactionEventWrapper(client, msg);
            wrap.OnReactionAdded += (r) => { var _ = Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { var _ = Task.Run(() => reactionRemoved(r)); };
            return wrap;
        }

        public static EmbedBuilder EmbedFromJson(string json)
        {
            try
            {
                var embed = new EmbedBuilder();
                var embedDeserialized = JsonConvert.DeserializeObject<JsonEmbed>(json);
                var color = (embedDeserialized.color == true) ? RiasBot.goodColor : RiasBot.badColor;
                embed.WithColor(color);
                embed.WithTitle(embedDeserialized.title);

                string description = embedDeserialized.description;
                description = description.Replace("[currency]", RiasBot.currency);
                description = description.Replace("%currency%", RiasBot.currency);
                embed.WithDescription(embedDeserialized.description);
                embed.WithThumbnailUrl(embedDeserialized.thumbnail);
                embed.WithImageUrl(embedDeserialized.image);
                try
                {
                    foreach (var field in embedDeserialized.fields)
                    {
                        embed.AddField(field.title, field.content, field.inline);
                    }
                }
                catch
                {

                }
                if (embedDeserialized.timestamp)
                    embed.WithCurrentTimestamp();
                return embed;
            }
            catch
            {
                return null;
            }
        }
    }

    public class JsonEmbed
    {
        public bool color { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string thumbnail { get; set; }
        public string image { get; set; }
        public EmbedFields[] fields { get; set; }
        public bool timestamp { get; set; }
    }

    public class EmbedFields
    {
        public string title { get; set; }
        public string content { get; set; }
        public bool inline { get; set; }
    }
}
