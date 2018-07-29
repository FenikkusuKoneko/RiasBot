using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Timers;

namespace RiasBot.Commons.TypeReaders
{
    public class UntilTimeTypeReader : RiasTypeReader<UntilTime>
    {
        public UntilTimeTypeReader(DiscordShardedClient client, CommandService cmds) : base(client, cmds)
        {
        }
        
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.Unsuccessful, "Input is empty."));
            try
            {
                var time = UntilTime.FromInput(input);
                return Task.FromResult(TypeReaderResult.FromSuccess(time));
            }
            catch (Exception ex)
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.Exception, ex.Message));
            }
        }
    }
}