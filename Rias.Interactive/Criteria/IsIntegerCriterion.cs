using System.Threading.Tasks;
using Discord;

namespace Rias.Interactive.Criteria
{
    internal class IsIntegerCriterion : ICriterion<IMessage>
    {
        public Task<bool> CheckAsync(IMessage userMessage, IMessage parameter)
        {
            return Task.FromResult(int.TryParse(parameter.Content, out _));
        }
    }
}