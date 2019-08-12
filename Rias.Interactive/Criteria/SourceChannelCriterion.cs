using System.Threading.Tasks;
using Discord;

namespace Rias.Interactive.Criteria
{
    internal class SourceChannelCriterion : ICriterion<IMessage>
    {
        public Task<bool> CheckAsync(IMessage userMessage, IMessage parameter)
        {
            return Task.FromResult(userMessage.Channel.Id == parameter.Channel.Id);
        }
    }
}