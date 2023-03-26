using Disqord;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qommon;
using Rias.Common;

namespace Rias.Services.TypeParsers;

// Taken from Disqord.Bot.Commands.Parsers.MemberTypeParser and adapted to support localisation

/// <summary>
///     Represents type parsing for the <see cref="IMember"/> type.
///     Supports parsing members that are not in the cache.
/// </summary>
/// <remarks>
///     Supports the following inputs, in order:
///     <list type="number">
///         <item>
///             <term> ID </term>
///             <description> The ID of the member. </description>
///         </item>
///         <item>
///             <term> Mention </term>
///             <description> The mention of the member. </description>
///         </item>
///         <item>
///             <term> Tag </term>
///             <description> The tag of the member. This is case-insensitive. </description>
///         </item>
///         <item>
///             <term> Name / Nick </term>
///             <description> The name or nick of the member. This is case-insensitive. </description>
///         </item>
///     </list>
/// </remarks>
public class MemberTypeParser : DiscordGuildTypeParser<IMember>
{
    public override async ValueTask<ITypeParserResult<IMember>> ParseAsync(IDiscordGuildCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        if (!context.Bot.CacheProvider.TryGetMembers(context.GuildId, out var memberCache))
            throw new InvalidOperationException($"The {GetType().Name} requires the member cache.");

        IMember? member;
        if (Helpers.TryParseUserId(value.Span, out var id))
        {
            // The value is a mention or an ID.
            // We look up the cache first.
            if (!memberCache.TryGetValue(id, out var cachedMember))
            {
                // This means it's either an invalid ID or the member isn't cached.
                // We don't know which one it is, so we have to query the guild.

                // Check if the gateway is/will be rate-limited.
                if (context.Bot.ApiClient.GetShard(context.GuildId)!.RateLimiter.GetRemainingRequests() < 3)
                {
                    // Use a REST call instead.
                    member = await context.Bot.FetchMemberAsync(context.GuildId, id);
                }
                else
                {
                    // Otherwise use gateway member chunking.
                    var members = await context.Bot.Chunker.QueryAsync(context.GuildId, new[] { id });
                    member = members.GetValueOrDefault(id);
                }
            }
            else
            {
                member = cachedMember;
            }
        }
        else
        {
            // The value is possibly a tag, name, or nick.
            var (name, discriminator) = Helpers.ParseTag(value);
            
            static IMember? FindMember(IReadOnlyCollection<IMember> members, string name, string? discriminator)
            {
                // Checks for tag.
                if (discriminator is not null)
                    return members.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.InvariantCultureIgnoreCase) && string.Equals(m.Discriminator, discriminator));

                // Checks for name and then nick.
                return members.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.InvariantCultureIgnoreCase))
                       ?? members.FirstOrDefault(m => string.Equals(m.Nick, name, StringComparison.InvariantCultureIgnoreCase));
            }

            member = FindMember(memberCache.Values, name, discriminator);
            if (member is null)
            {
                // This means it's either an invalid input or the member isn't cached.

                // We don't know which one it is, so we have to query the guild.
                // Check if the gateway is/will be rate-limited.
                IEnumerable<IMember> members;
                if (context.Bot.ApiClient.GetShard(context.GuildId)!.RateLimiter.GetRemainingRequests() < 3)
                    members = await context.Bot.SearchMembersAsync(context.GuildId, name);
                else
                    members = (await context.Bot.Chunker.QueryAsync(context.GuildId, name)).Values;

                member = FindMember(members.ToList(), name, discriminator);
            }
        }

        if (member != null)
            return Success(new Optional<IMember>(member));

        var localisation = context.Services.GetRequiredService<LocalisationService>();
        return Failure(localisation.GetText(context.GuildId, Strings.TypeParser.MemberNotFound));
    }
}