using Disqord;

namespace Rias.Services.Responses.Administration;

public record SetGreetMessageResponse
{
    public bool IsGreetEnabled { get; init; }
    public IGuildChannel? Channel { get; init; }
}