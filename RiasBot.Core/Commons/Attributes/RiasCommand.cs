using System.Runtime.CompilerServices;
using Discord.Commands;
using RiasBot.Services.Implementation;

namespace RiasBot.Commons.Attributes
{
    public class RiasCommand : CommandAttribute
    {
        public RiasCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Command.Split(' ')[0])
        {

        }
    }
}