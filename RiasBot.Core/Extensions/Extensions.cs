using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using RiasBot.Commons;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;

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

        public static EmbedBuilder EmbedFromJson(string json)
        {
            try
            {
                var embed = new EmbedBuilder();
                var embedDeserialized = JsonConvert.DeserializeObject<JsonEmbed>(json);
                var color = (embedDeserialized.color == true) ? RiasBot.GoodColor : RiasBot.BadColor;
                embed.WithColor(color);
                embed.WithTitle(embedDeserialized.title);

                var description = embedDeserialized.description;
                description = description.Replace("[currency]", RiasBot.Currency);
                description = description.Replace("%currency%", RiasBot.Currency);
                embed.WithDescription(description);
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

        public static string ToTitleCase(this string input)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
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
