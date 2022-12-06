using System.Globalization;
using System.Text;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Newtonsoft.Json;
using Rias.Common.Models;

namespace Rias.Common;

public static class Helpers
{
    public static string Stringify(this IPrefix prefix)
        => char.IsLetter(prefix.ToString()![^1]) ? prefix + " " : prefix.ToString()!;
    
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

    public static string? FormatPlaceholders(IUser user, string? message)
    {
        if (message is null)
            return null;
        
        var username = user.Tag.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var sb = new StringBuilder(message)
            .Replace("%member%", username)
            .Replace("%user%", username)
            .Replace("%avatar%", user.GetAvatarUrl(CdnAssetFormat.Automatic, 2048));

        if (user is IMember member)
        {
            var guild = member.GetGuild();
            
            var guildName = guild?.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
            sb.Replace("%mention%", member.Mention)
                .Replace("%server%", guildName)
                .Replace("%members%", guild?.MemberCount.ToString() ?? "0");
        }

        return sb.ToString();
    }

    public static bool TryParseMessage(string? json, out string? content, out LocalEmbed? embed)
    {
        if (string.IsNullOrEmpty(json))
        {
            content = default;
            embed = default;
            return false;
        }
        
        JsonEmbed jsonEmbed;

        try
        {
            jsonEmbed = JsonConvert.DeserializeObject<JsonEmbed>(json);
        }
        catch
        {
            content = default;
            embed = default;
            return false;
        }

        content = jsonEmbed.Content;

        if (jsonEmbed.IsEmbedEmpty())
        {
            embed = default;
            return true;
        }

        try
        {
            embed = new LocalEmbed();

            var author = jsonEmbed.Author;
            var title = jsonEmbed.Title;
            var description = jsonEmbed.Description;

            var colorString = jsonEmbed.Color;
            var thumbnail = jsonEmbed.Thumbnail;
            var image = jsonEmbed.Image;
            var fields = jsonEmbed.Fields;
            var footer = jsonEmbed.Footer;
            var timestamp = jsonEmbed.Timestamp;

            if (author.HasValue && !string.IsNullOrEmpty(author.Value.Name))
                embed.WithAuthor(author.Value.Name, author.Value.Url, author.Value.IconUrl);

            if (!string.IsNullOrEmpty(title))
                embed.WithTitle(title);

            if (!string.IsNullOrEmpty(description))
                embed.WithDescription(description);

            if (!string.IsNullOrEmpty(colorString))
            {
                if (string.Equals(colorString, "random", StringComparison.OrdinalIgnoreCase))
                    embed.WithColor(Color.Random);
                else
                    embed.WithColor(HexToInt(colorString) ?? 0xFFFFFF);
            }

            if (!string.IsNullOrEmpty(thumbnail))
                embed.WithThumbnailUrl(thumbnail);
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

            if (footer.HasValue && !string.IsNullOrEmpty(footer.Value.Text))
                embed.WithFooter(footer.Value.Text, footer.Value.IconUrl);

            if (timestamp.HasValue)
                embed.WithTimestamp(timestamp.Value);
            else if (jsonEmbed.WithCurrentTimestamp)
                embed.WithTimestamp(DateTimeOffset.UtcNow);

            return true;
        }
        catch
        {
            embed = default;
            return false;
        }
    }
}