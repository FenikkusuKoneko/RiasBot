using System.Threading.Tasks;
using Disqord;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Rias.Core.Implementation
{
    public class RiasPagedMenu : PagedMenu
    {
        public RiasPagedMenu(Snowflake userId, IPageProvider pageProvider) : base(userId, pageProvider, false)
        {
            
        }

        public override async ValueTask DisposeAsync()
        {
            if (await Channel.GetMessageAsync(Message.Id) is null)
                return;
            
            await Message.ClearReactionsAsync();
        }
    }
}