using Disqord;

namespace Rias.Services.Responses.Administration;

public record struct SetGreetResponse
{
    public bool IsGreetEnabled { get; init; }
    public string? Content { get; init; }
    public LocalEmbed? Embed { get; init; }
}