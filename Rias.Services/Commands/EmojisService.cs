using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Rias.Common;
using Rias.Database;
using Rias.Services.Extensions;
using Rias.Services.Responses;

namespace Rias.Services.Commands;

public class EmojisService : RiasCommandService
{
    private readonly HttpClient _httpClient;
    
    public EmojisService(
        RiasDbContext db,
        LocalisationService localisation,
        HttpClient httpClient)
        : base(db, localisation)
    {
        _httpClient = httpClient;
    }

    public async Task<RiasResult<IGuildEmoji>> AddEmojiAsync(ICustomEmoji emoji, CachedGuild guild, string name)
    {
        var emojiSlots = guild.GetEmojiSlots();
        
        if (emoji.IsAnimated)
        {
            if (guild.Emojis.Count(e => e.Value.IsAnimated) == emojiSlots)
                return ErrorResult<IGuildEmoji>(guild.Id, Strings.Administration.AnimatedEmojisLimit, emojiSlots);
        }
        else
        {
            if (guild.Emojis.Count(e => !e.Value.IsAnimated) == emojiSlots)
                return ErrorResult<IGuildEmoji>(guild.Id, Strings.Administration.NonAnimatedEmojisLimit, emojiSlots);
        }
        
        var url = emoji.GetUrl(CdnAssetFormat.Automatic, 128);

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            return ErrorResult<IGuildEmoji>(guild.Id, Strings.Administration.EmojiNotFound);

        var image = await response.Content.ReadAsStreamAsync();
        name = name.Replace(" ", "");
        var newEmoji = await guild.CreateEmojiAsync(name, image);

        return SuccessResult(newEmoji);
    }

    public async Task<RiasResult<IGuildEmoji>> AddEmojiAsync(string url, CachedGuild guild, string name)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return ErrorResult<IGuildEmoji>(guild.Id, Strings.Utility.UrlNotValid);
        
        if (uri.Scheme != Uri.UriSchemeHttps)
            return ErrorResult<IGuildEmoji>(guild.Id, Strings.Utility.UrlNotHttps);
        
        if (!Helpers.IsPngJpgWebpGif(uri))
            return ErrorResult<IGuildEmoji>(guild.Id, Strings.Utility.UrlNotPngJpgWebpGif);

        try
        {
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return ErrorResult<IGuildEmoji>(guild.Id, Strings.Utility.InvalidImageUrl);
            
            var image = await response.Content.ReadAsStreamAsync();
            if (!(Helpers.IsPng(image) || Helpers.IsJpg(image) || Helpers.IsWebp(image)))
                return ErrorResult<IGuildEmoji>(guild.Id, Strings.Utility.InvalidImageUrl);

            var isAnimated = Helpers.IsGif(image);
            var emojiSlots = guild.GetEmojiSlots();
        
            if (isAnimated)
            {
                if (guild.Emojis.Count(e => e.Value.IsAnimated) == emojiSlots)
                    return ErrorResult<IGuildEmoji>(guild.Id, Strings.Administration.AnimatedEmojisLimit, emojiSlots);
            }
            else
            {
                if (guild.Emojis.Count(e => !e.Value.IsAnimated) == emojiSlots)
                    return ErrorResult<IGuildEmoji>(guild.Id, Strings.Administration.NonAnimatedEmojisLimit, emojiSlots);
            }
            
            image.Position = 0;
            name = name.Replace(" ", "");
            var newEmoji = await guild.CreateEmojiAsync(name, image);

            return SuccessResult(newEmoji);
        }
        catch (HttpRequestException)
        {
            return ErrorResult<IGuildEmoji>(guild.Id, Strings.Utility.CannotAccessImageUrl,
                _httpClient.MaxResponseContentBufferSize.BytesToKilobytes(), DataExtensions.Kilobytes);
        }
    }
}