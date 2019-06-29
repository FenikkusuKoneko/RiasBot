using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using Newtonsoft.Json;
using Rias.Core.Commons;

namespace Rias.Core.Implementation
{
    public class RiasUtils
    {
        // these are the color used by the confirmation message embed and error message embed
        // these are modified from the Credentials class
        
        public static uint ConfirmColor = 0x00ff00;
        public static uint ErrorColor = 0xff0000;
        
        /// <summary>
        ///     Convert a string to TimeSpan.<br/>
        ///     Example 1mo2w3d4h5m6s to TimeSpan.<br/>
        ///     It will return TimeSpan.Zero if the input doesn't match the Regex (mo w d m s).
        /// </summary>
        /// <param name="input"></param>
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
        
        public static uint HexToUint(string hex)
        {
            hex = hex?.Replace("#", "");
            if (string.IsNullOrWhiteSpace(hex)) return 0xFFFFFF;
            return uint.TryParse(hex, NumberStyles.HexNumber, null, out var result) ? result : 0xFFFFFF;
        }
        
        public static bool TryParseEmbed(string json, out EmbedBuilder embed, Credentials creds = null)
        {
            embed = new EmbedBuilder();
            try
            {
                var embedDeserialized = JsonConvert.DeserializeObject<JsonEmbed>(json);

                var author = embedDeserialized.Author;
                var title = embedDeserialized.Title;
                var description = embedDeserialized.Description;

                var colorString = embedDeserialized.Color;
                var thumbnail = embedDeserialized.Thumbnail;
                var image = embedDeserialized.Image;
                var fields = embedDeserialized.Fields;
                var footer = embedDeserialized.Footer;
                var timestamp = embedDeserialized.Timestamp;

                if (author != null)
                {
                    embed.WithAuthor(author);
                }

                if (!string.IsNullOrEmpty(title))
                {
                    embed.WithTitle(title);
                }

                if (!string.IsNullOrEmpty(description))
                {
                    if (creds != null)
                    {
                        description = description.Replace("[currency]", creds.Currency);
                        description = description.Replace("%currency%", creds.Currency);
                    }

                    embed.WithDescription(description);
                }

                if (!string.IsNullOrEmpty(colorString))
                {
                    colorString = colorString.Replace("#", "");
                    var color = HexToUint(colorString);
                    embed.WithColor(color);
                }

                if (!string.IsNullOrEmpty(thumbnail))
                    embed.WithThumbnailUrl(thumbnail);
                if (!string.IsNullOrEmpty(image))
                    embed.WithImageUrl(image);

                if (fields != null)
                {
                    foreach (var field in embedDeserialized.Fields)
                    {
                        var fieldName = field.Name;
                        var fieldValue = field.Value;
                        var fieldInline = field.Inline;

                        if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldValue))
                        {
                            embed.AddField(fieldName, fieldValue, fieldInline);
                        }
                    }
                }


                if (footer != null)
                {
                    embed.WithFooter(footer);
                }

                if (!timestamp.Equals(DateTimeOffset.MinValue))
                {
                    embed.WithTimestamp(timestamp);
                }
                else
                {
                    if (embedDeserialized.WithCurrentTimestamp)
                        embed.WithCurrentTimestamp();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}