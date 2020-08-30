using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Rias.Core.Models;

namespace Rias.Core.Implementation
{
    public static class RiasUtilities
    {
        // these are the color used by the confirmation message embed and error message embed
        // these are modified from the Credentials class
        
        public static DiscordColor Red = new DiscordColor(0xFF0000);
        public static DiscordColor Green = new DiscordColor(0x00FF00);
        public static DiscordColor Yellow = new DiscordColor(0xFFFF00);
        
        public static DiscordColor ConfirmColor = Green;
        public static DiscordColor ErrorColor = Red;

        private static readonly Regex TimeSpanRegex = new Regex(
            @"^(?:(?<months>\d{1,2})mo)?(?:(?<weeks>\d{1,3})w)?(?:(?<days>\d{1,3})d)?(?:(?<hours>\d{1,4})h)?(?:(?<minutes>\d{1,5})m)?(?:(?<seconds>\d{1,5})s)?$",
            RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex UserRegex = new Regex(@"^<@\!?(\d+?)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex RoleRegex = new Regex(@"^<@&(\d+?)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex ChannelRegex = new Regex(@"^<#(\d+)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex EmojiRegex = new Regex(@"^<a?:.+:(\d+)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);

        /// <summary>
        /// Checks if the user's message has the bot mention
        /// </summary>
        public static bool HasMentionPrefix(this DiscordMessage message, DiscordUser currentUser, out string output)
        {
            var contentSpan = message.Content.AsSpan();
            if (contentSpan.Length > 17 && contentSpan[0] == '<' && contentSpan[1] == '@')
            {
                var closingBracketIndex = contentSpan.IndexOf('>');
                if (closingBracketIndex != -1)
                {
                    var idSpan = contentSpan[2] == '!'
                        ? contentSpan.Slice(3, closingBracketIndex - 3)
                        : contentSpan.Slice(2, closingBracketIndex - 2);
                    if (ulong.TryParse(idSpan, out var id) && id == currentUser.Id)
                    {
                        output = new string(contentSpan.Slice(closingBracketIndex + 1));
                        return true;
                    }
                }
            }

            output = string.Empty;
            return false;
        }

        public static bool TryParseUserMention(string value, out ulong userId)
        {
            var user = UserRegex.Match(value);
            if (user.Success && ulong.TryParse(user.Groups[1].Value, out userId))
                return true;
            
            userId = 0;
            return false;
        }
        
        public static bool TryParseRoleMention(string value, out ulong roleId)
        {
            var role = RoleRegex.Match(value);
            if (role.Success && ulong.TryParse(role.Groups[1].Value, out roleId))
                return true;

            roleId = 0;
            return false;
        }
        
        public static bool TryParseChannelMention(string value, out ulong channelId)
        {
            var channel = ChannelRegex.Match(value);
            if (channel.Success && ulong.TryParse(channel.Groups[1].Value, out channelId))
                return true;

            channelId = 0;
            return false;
        }
        
        public static bool TryParseEmoji(string value, out ulong emojiId)
        {
            var emoji = EmojiRegex.Match(value);
            if (emoji.Success && ulong.TryParse(emoji.Groups[1].Value, out emojiId))
                return true;

            emojiId = 0;
            return false;
        }

        /// <summary>
        ///     Convert a string to TimeSpan.<br/>
        ///     Example 1mo2w3d4h5m6s to TimeSpan.<br/>
        ///     It will return null if the input doesn't match the Regex (mo w d m s).
        /// </summary>
        /// <param name="input"></param>
        public static TimeSpan? ConvertToTimeSpan(string input)
        {
            var match = TimeSpanRegex.Match(input);

            if (match.Length == 0)
                return null;

            var timeValues = new Dictionary<string, int>();

            foreach (var group in match.Groups.Values)
            {
                if (!int.TryParse(group.Value, out var value))
                {
                    timeValues[group.Name] = 0;
                    continue;
                }

                timeValues[group.Name] = value;
            }

            var now = DateTime.UtcNow;
            var time = now.AddMonths(timeValues["months"])
                .AddDays(timeValues["weeks"] * 7 + timeValues["days"])
                .AddHours(timeValues["hours"])
                .AddMinutes(timeValues["minutes"])
                .AddSeconds(timeValues["seconds"]);

            return time - now;
        }

        public static int? HexToInt(string? hex, int decimals = 6)
        {
            hex = hex?.Replace("#", string.Empty);

            if (string.IsNullOrEmpty(hex))
                return null;

            if (hex.Length != decimals)
                return null;

            if (int.TryParse(hex, NumberStyles.HexNumber, null, out var result))
                return result;

            return null;
        }
        
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> enumerable, int size)  
        {        
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
                yield return InternalSplit(enumerator, size);
        }

        private static IEnumerable<T> InternalSplit<T>(IEnumerator<T> enumerator, int size)
        {
            do
                yield return enumerator.Current;
            while (--size > 0 && enumerator.MoveNext());
        }

        public static DiscordEmbedBuilder WithCurrentTimestamp(this DiscordEmbedBuilder embedBuilder)
            => embedBuilder.WithTimestamp(DateTimeOffset.UtcNow);

        public static bool TryParseMessage(string json, out CustomMessage message)
        {
            message = new CustomMessage();
            JsonEmbed jsonEmbed;

            try
            {
                jsonEmbed = JsonConvert.DeserializeObject<JsonEmbed>(json);
            }
            catch
            {
                return false;
            }

            message.Content = jsonEmbed.Content;
            if (jsonEmbed.IsEmbedEmpty())
                return true;

            try
            {
                var embed = new DiscordEmbedBuilder();

                var author = jsonEmbed.Author;
                var title = jsonEmbed.Title;
                var description = jsonEmbed.Description;

                var colorString = jsonEmbed.Color;
                var thumbnail = jsonEmbed.Thumbnail;
                var image = jsonEmbed.Image;
                var fields = jsonEmbed.Fields;
                var footer = jsonEmbed.Footer;
                var timestamp = jsonEmbed.Timestamp;

                if (author != null)
                    embed.WithAuthor(author.Name, author.Url, author.IconUrl);

                if (!string.IsNullOrEmpty(title))
                    embed.WithTitle(title);

                if (!string.IsNullOrEmpty(description))
                    embed.WithDescription(description);

                if (!string.IsNullOrEmpty(colorString))
                    embed.WithColor(HexToInt(colorString) ?? 0xFFFFFF);

                if (!string.IsNullOrEmpty(thumbnail))
                    embed.WithThumbnail(thumbnail);
                if (!string.IsNullOrEmpty(image))
                    embed.WithImageUrl(image);

                if (fields != null)
                {
                    foreach (var field in fields)
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
                    embed.WithFooter(footer.Text, footer.IconUrl);

                if (timestamp.HasValue)
                    embed.WithTimestamp(timestamp.Value);
                else if (jsonEmbed.WithCurrentTimestamp)
                    embed.WithCurrentTimestamp();

                message.Embed = embed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Calculates and returns the level from a total xp based on a threshold.<br/>
        /// Note: The level starts from 0.
        /// </summary>
        public static int XpToLevel(int xp, int threshold)
            => (int) (1 + Math.Sqrt(1 + 8 * xp / threshold)) / 2 - 1;

        /// <summary>
        /// Calculates and returns the xp accumulated for a level from the total xp based on a threshold.
        /// Note: The level is considered that starts from 0.
        /// </summary>
        public static int LevelXp(int level, int xp, int threshold)
            => xp - (level + 1) * threshold * level / 2;

        /// <summary>
        /// Checks the header of a stream if is PNG.
        /// </summary>
        public static bool IsPng(Stream stream)
            => HasHeader(stream, new[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A});

        /// <summary>
        /// Checks the header of a stream if is JPG/JPEG.
        /// </summary>
        public static bool IsJpg(Stream stream)
            => HasHeader(stream, new[] {0xFF, 0xD8, 0xFF, 0xDB})
               || HasHeader(stream, new[] {0xFF, 0xD8, 0xFF, 0xE0, 0x0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x0, 0x01})
               || HasHeader(stream, new[] {0xFF, 0xD8, 0xFF, 0xEE})
               || HasHeader(stream, new[] {0xFF, 0xD8, 0xFF, 0xE1, -1, -1, 0x45, 0x78, 0x69, 0x66, 0x0, 0x0});

        /// <summary>
        /// Checks the header of a stream if is GIF.
        /// </summary>
        public static bool IsGif(Stream stream)
            => HasHeader(stream, new[] {0x47, 0x49, 0x46, 0x38, 0x37, 0x61})
               || HasHeader(stream, new[] {0x47, 0x49, 0x46, 0x38, 0x39, 0x61});

        private static bool HasHeader(Stream stream, IEnumerable<int> headerValues)
        {
            if (!(stream.CanRead || stream.CanSeek))
                return false;

            stream.Position = 0;
            foreach (var value in headerValues)
            {
                var @byte = stream.ReadByte();
                if (value == -1)    //skip this byte
                    continue;
                
                if (@byte != value)
                    return false;
            }

            return true;
        }
    }
}