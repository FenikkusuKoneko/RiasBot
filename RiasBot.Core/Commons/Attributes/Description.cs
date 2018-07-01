using System.Runtime.CompilerServices;
using Discord.Commands;
using RiasBot.Services.Implementation;

namespace RiasBot.Commons.Attributes
{
    public class Description : SummaryAttribute
    {
        public Description([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Description)
        {

        }
    }
}
