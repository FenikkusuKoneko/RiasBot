using System.Threading.Tasks;
using Discord;

namespace Rias.Interactive.Criteria
{
    public interface ICriterion<in T>
    {
        Task<bool> CheckAsync(IMessage userMessage, T parameter);
    }
}