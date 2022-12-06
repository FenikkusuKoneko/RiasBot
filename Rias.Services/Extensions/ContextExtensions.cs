using Disqord;
using Disqord.Bot.Commands.Text;
using Disqord.Gateway;

namespace Rias.Services.Extensions;

public static class ContextExtensions
{
    public static CachedGuild GetGuild(this IDiscordTextGuildCommandContext context)
        => context.Bot.GetGuild(context.GuildId) ?? throw new InvalidOperationException($"The guild ({context.GuildId}) is not cached");
    
    public static IMessageGuildChannel GetChannel(this IDiscordTextGuildCommandContext context)
        => context.Channel ?? throw new InvalidOperationException($"The message channel ({context.ChannelId}) is not cached");
    
    public static CachedMember GetCurrentMember(this IDiscordTextGuildCommandContext context)
        => context.Bot.GetCurrentMember(context.GuildId) ?? throw new InvalidOperationException($"The current member is not cached in the guild ({context.GuildId})");
}