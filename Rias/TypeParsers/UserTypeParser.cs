using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Implementation;

namespace Rias.TypeParsers
{
    public class UserTypeParser : RiasTypeParser<DiscordUser>
    {
        public override async ValueTask<TypeParserResult<DiscordUser>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var cachedMemberTypeParser = await new MemberTypeParser().ParseAsync(parameter, value, context);
            if (cachedMemberTypeParser.IsSuccessful)
                return TypeParserResult<DiscordUser>.Successful(cachedMemberTypeParser.Value);
            
            var localization = context.ServiceProvider.GetRequiredService<Localization>();

            if (ulong.TryParse(value, out var id))
            {
                try
                {
                    var user = await context.Client.GetUserAsync(id);
                    return TypeParserResult<DiscordUser>.Successful(user);
                }
                catch
                {
                    return TypeParserResult<DiscordUser>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationUserNotFound));
                }
            }
            
            return TypeParserResult<DiscordUser>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationUserNotFound));
        }
    }
}