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

    public static bool TryParseUserId(ReadOnlySpan<char> value, out Snowflake id)
        => Snowflake.TryParse(value, out id) || Mention.TryParseUser(value, out id);

    public static (string, string?) ParseTag(ReadOnlyMemory<char> value)
    {
        string name;
        string? discriminator;
        var valueSpan = value.Span;
        var hashIndex = valueSpan.LastIndexOf('#');
        if (hashIndex != -1 && hashIndex + 5 == value.Length)
        {
            // The value is a tag (Name#0000);
            name = new string(valueSpan[..(value.Length - 5)]);
            discriminator = new string(valueSpan[(hashIndex + 1)..]);
        }
        else
        {
            // The value is possibly a name or a nick.
            name = value.ToString();
            discriminator = null;
        }

        return (name, discriminator);
    }

    public static bool TryParseMessage(string? json, out string? content, out LocalEmbed? embed)
    {
        if (string.IsNullOrEmpty(json))
        {
            content = default;
            embed = default;
            return false;
        }

        JsonEmbed? jsonEmbed;

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

        if (jsonEmbed is null)
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

            if (!string.IsNullOrEmpty(author?.Name))
                embed.WithAuthor(author.Name, author.Url, author.IconUrl);

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
                        embed.AddField(fieldName, fieldValue, fieldInline);
                }
            }

            if (!string.IsNullOrEmpty(footer?.Text))
                embed.WithFooter(footer.Text, footer.IconUrl);

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

    public static bool IsPngJpgWebp(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension is ".png" or ".jpg" or ".webp";
    }

    public static bool IsPngJpgWebpGif(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension is ".png" or ".jpg" or ".webp" or ".gif";
    }

    /// <summary>
    /// Checks the header of a stream if is PNG.<br/>
    /// 89 50 4E 47 0D 0A 1A 0A.
    /// </summary>
    public static bool IsPng(Stream stream)
        => HasHeader(stream, new[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

    /// <summary>
    /// Checks the header of a stream if is JPG/JPEG.<br/>
    /// FF D8 FF E0 00 10 4A 46 49 46 00 01.<br/>
    /// FF D8 FF EE.<br/>
    /// FF D8 FF E1 ?? ?? 45 78 69 66 00 00, where ?? ?? is the length of the EXIF data.
    /// </summary>
    public static bool IsJpg(Stream stream)
        => HasHeader(stream, new[] { 0xFF, 0xD8, 0xFF, 0xDB })
           || HasHeader(stream, new[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x0, 0x01 })
           || HasHeader(stream, new[] { 0xFF, 0xD8, 0xFF, 0xEE })
           || HasHeader(stream, new[] { 0xFF, 0xD8, 0xFF, 0xE1, -1, -1, 0x45, 0x78, 0x69, 0x66, 0x0, 0x0 });

    /// <summary>
    /// Checks the header of a stream if is WEBP.<br/>
    /// 52 49 46 46 ?? ?? ?? ?? 57 45 42 50, where ?? ?? ?? ?? is the size of the file - 8 bytes.
    /// </summary>
    public static bool IsWebp(Stream stream)
        => HasHeader(stream, new[] { 0x52, 0x49, 0x46, 0x46, -1, -1, -1, -1, 0x57, 0x45, 0x42, 0x50 });

    /// <summary>
    /// Checks the header of a stream if is GIF.<br/>
    /// 47 49 46 38 39 61, GIF87a.<br/>
    /// 47 49 46 38 39 61, GIF89a.
    /// </summary>
    public static bool IsGif(Stream stream)
        => HasHeader(stream, new[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 })
           || HasHeader(stream, new[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 });

    private static bool HasHeader(Stream stream, IEnumerable<int> values)
    {
        if (!(stream.CanRead || stream.CanSeek))
            return false;

        stream.Position = 0;
        foreach (var value in values)
        {
            var @byte = stream.ReadByte();
            if (value == -1) // ? ? bytes, can be anything
                continue;

            if (@byte != value)
                return false;
        }

        return true;
    }
}