using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class UserTypeParser : RiasTypeParser<IUser>
    {
        public override async ValueTask<TypeParserResult<IUser>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var cachedMemberTypeParser = await new CachedMemberTypeParser().ParseAsync(parameter, value, context);
            if (cachedMemberTypeParser.IsSuccessful)
                return TypeParserResult<IUser>.Successful(cachedMemberTypeParser.Value);
            
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            IUser? user = null;

            if (ulong.TryParse(value, out var id))
                user = await context.ServiceProvider.GetRequiredService<Rias>().GetUserAsync(id);
            
            if (user != null)
                return TypeParserResult<IUser>.Successful(user);
                
            if (parameter.IsOptional)
                return TypeParserResult<IUser>.Successful((CachedMember) parameter.DefaultValue);
            
            return TypeParserResult<IUser>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationUserNotFound));
        }
    }
}