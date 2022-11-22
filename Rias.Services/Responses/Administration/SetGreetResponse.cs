using Disqord;

namespace Rias.Services.Responses.Administration;

public record struct SetGreetResponse
{
    public bool GreetEnabled { get; init; }
    public string? Content { get; init; }
    public LocalEmbed? Embed { get; init; }
}