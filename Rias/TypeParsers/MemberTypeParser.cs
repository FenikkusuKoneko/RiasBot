using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Implementation;
using Rias.Services;
using Serilog;

namespace Rias.TypeParsers
{
    public class MemberTypeParser : RiasTypeParser<DiscordMember>
    {
        public override async ValueTask<TypeParserResult<DiscordMember>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.Services.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<DiscordMember>.Failed(localization.GetText(context.Guild?.Id, Localization.TypeParserMemberNotGuild));

            var riasBot = context.Services.GetRequiredService<RiasBot>();
            if (!riasBot.ChunkedGuilds.Contains(context.Guild.Id))
            {
                var botService = context.Services.GetRequiredService<BotService>();
                var tcs = new TaskCompletionSource();
                botService.GuildsTcs[context.Guild.Id] = tcs;
                
                riasBot.ChunkedGuilds.Add(context.Guild.Id);
                await context.Guild.RequestMembersAsync();
                Log.Debug("Members requested for {GuildName} ({GuildId})", context.Guild.Name, context.Guild.Id);

                var delayTimeout = context.Guild.MemberCount switch
                {
                    <= 1000 => 2000,
                    <= 5000 => 3000,
                    _ => 5000
                };
                
                var delay = Task.Delay(delayTimeout);

                await Task.WhenAny(tcs.Task, delay);
                botService.GuildsTcs.TryRemove(context.Guild.Id, out _);
            }

            DiscordMember? member;
            if (RiasUtilities.TryParseUserMention(value, out var memberId) || ulong.TryParse(value, out memberId))
            {
                var bot = context.Services.GetRequiredService<RiasBot>();
                member = await bot.GetMemberAsync(context.Guild, memberId);
                
                return member is not null
                    ? TypeParserResult<DiscordMember>.Successful(member)
                    : TypeParserResult<DiscordMember>.Failed(localization.GetText(context.Guild?.Id, Localization.AdministrationMemberNotFound));
            }

            var members = context.Guild.Members;

            var index = value.LastIndexOf("#", StringComparison.Ordinal);
            if (index > 0)
            {
                var username = value[..index];
                var discriminator = value[(index + 1)..];
                if (discriminator.Length == 4 && int.TryParse(discriminator, out _))
                {
                    member = members.FirstOrDefault(u => string.Equals(u.Value.Discriminator, discriminator)
                                                         && string.Equals(u.Value.Username, username, StringComparison.OrdinalIgnoreCase)).Value;
                    if (member != null)
                        return TypeParserResult<DiscordMember>.Successful(member);
                }
            }

            member = members.FirstOrDefault(u => string.Equals(u.Value.Nickname, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (member != null)
                return TypeParserResult<DiscordMember>.Successful(member);

            member = members.FirstOrDefault(u => string.Equals(u.Value.Username, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (member != null)
                return TypeParserResult<DiscordMember>.Successful(member);
            
            return TypeParserResult<DiscordMember>.Failed(localization.GetText(context.Guild?.Id, Localization.AdministrationMemberNotFound));
        }
    }
}