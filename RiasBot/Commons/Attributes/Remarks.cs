using System.Runtime.CompilerServices;
using Discord.Commands;
using RiasBot.Services.Implementation;
using System.Linq;
using Discord;

namespace RiasBot.Commons.Attributes
{
    public class Remarks : RemarksAttribute
    {
        public Remarks([CallerMemberName] string memberName="") : base(GetUsage(memberName))
        {

        }

        public static string GetUsage(string memberName)
        {
            var usage = Localization.LoadCommand(memberName.ToLowerInvariant()).Remarks;
            return string.Join(" or ", usage
                .Select(x => Format.Bold(x)));
        }
    }
}
