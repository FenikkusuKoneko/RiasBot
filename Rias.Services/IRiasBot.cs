using Disqord;

namespace Rias.Services;

public interface IRiasBot
{
    string Version { get; }
    string Author { get; }
    Snowflake AuthorId { get; }
    TimeSpan ElapsedTime { get; }
}