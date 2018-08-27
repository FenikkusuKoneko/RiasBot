using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using RiasBot.Commons;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RiasBot.Commons.Timers;
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

        public static string StringTimeSpan(this TimeSpan timeSpan)
        {
            var format = "";
            if (timeSpan.Days > 0)
                format += $"{timeSpan.Days}d";
            if (timeSpan.Hours > 0)
                format += $" {timeSpan.Hours}h";
            if (timeSpan.Minutes > 0)
                format += $" {timeSpan.Minutes}m";
            if (timeSpan.Hours > 0)
                format += $" {timeSpan.Seconds}s";
            return format;
        }
        
        public static string GetTimeString(this TimeSpan timeSpan)
        {
            var hoursInt = (int) timeSpan.TotalHours;
            var minutesInt = timeSpan.Minutes;
            var secondsInt = timeSpan.Seconds;

            var hours = hoursInt.ToString();
            var minutes = minutesInt.ToString();
            var seconds = secondsInt.ToString();

            if (hoursInt < 10)
                hours = "0" + hours;
            if (minutesInt < 10)
                minutes = "0" + minutes;
            if (secondsInt < 10)
                seconds = "0" + seconds;

            return hours + ":" + minutes + ":" + seconds;
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

        public static TimeSpan ConvertToTimeSpan(string input)
        {
            var regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d{1,2})w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,4})h)?(?:(?<minutes>\d{1,5})m)?(?:(?<seconds>\d{1,5})s)?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
            
            var match = regex.Match(input);

            if (match.Length == 0)
                return TimeSpan.Zero;

            var timeValues = new Dictionary<string, int>(); 
            
            foreach (var group in regex.GetGroupNames())
            {
                if (group == "0") continue;
                if (!int.TryParse(match.Groups[group].Value, out var value))
                {
                    timeValues[group] = 0;
                    continue;
                }

                timeValues[group] = value;
            }
            
            return new TimeSpan(30 * timeValues["months"] + 7 * timeValues["weeks"] + timeValues["days"],
                timeValues["hours"], timeValues["minutes"], timeValues["seconds"]);
        }

        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            var tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
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
