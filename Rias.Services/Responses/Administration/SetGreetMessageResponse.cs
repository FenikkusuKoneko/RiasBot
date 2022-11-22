using Disqord;

namespace Rias.Services.Responses.Administration;

public record struct SetGreetMessageResponse
{
    public bool GreetEnabled { get; init; }
    public IGuildChannel? Channel { get; init; }
}