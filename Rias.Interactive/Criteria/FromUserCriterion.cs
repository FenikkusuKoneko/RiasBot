using System.Threading.Tasks;
using Discord;

namespace Rias.Interactive.Criteria
{
    internal class FromUserCriterion : ICriterion<IMessage>
    {
        private readonly ulong _userId;

        public FromUserCriterion(IUser user)
        {
            _userId = user.Id;
        }

        public FromUserCriterion(ulong userId)
        {
            _userId = userId;
        }

        public Task<bool> CheckAsync(IMessage userMessage, IMessage parameter)
        {
            return Task.FromResult(_userId == parameter.Author.Id);
        }
    }
}