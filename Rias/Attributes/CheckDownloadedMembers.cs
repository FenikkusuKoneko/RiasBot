using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Commons;
using Rias.Implementation;
using Serilog;

namespace Rias.Attributes
{
    /// <summary>
    /// Checks if the guild's members are downloaded. If not, they will be requested.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CheckDownloadedMembers : RiasCheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (context.Guild is null)
            {
                return context.Command.Checks.Any(x => x is ContextAttribute contextAttribute && contextAttribute.Contexts.HasFlag(ContextType.Guild))
                    ? CheckResult.Successful
                    : CheckResult.Failed("Cannot use `CheckDownloadedMembers` outside of a guild.");
            }
            
            var riasBot = context.Services.GetRequiredService<RiasBot>();
            if (!riasBot.ChunkedGuilds.Contains(context.Guild.Id))
            {
                riasBot.ChunkedGuilds.Add(context.Guild.Id);
                await context.Guild.RequestMembersAsync();
                Log.Debug("Members requested for {GuildName} ({GuildId})", context.Guild.Name, context.Guild.Id);
            }

            return CheckResult.Successful;
        }
    }
}