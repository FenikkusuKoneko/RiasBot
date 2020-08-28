using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class UserTypeParser : RiasTypeParser<DiscordUser>
    {
        public override async ValueTask<TypeParserResult<DiscordUser>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var cachedMemberTypeParser = await new CachedMemberTypeParser().ParseAsync(parameter, value, context);
            if (cachedMemberTypeParser.IsSuccessful)
                return TypeParserResult<DiscordUser>.Successful(cachedMemberTypeParser.Value);
            
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            DiscordUser? user = null;

            if (ulong.TryParse(value, out var id))
                user = await context.Client.GetUserAsync(id);
            
            if (user != null)
                return TypeParserResult<DiscordUser>.Successful(user);

            return TypeParserResult<DiscordUser>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationUserNotFound));
        }
    }
}