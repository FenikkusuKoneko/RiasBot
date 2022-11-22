using Disqord;

namespace Rias.Services.Responses.Administration;

public class SetByeResponse
{
    public bool ByeEnabled { get; init; }
    public string? Content { get; init; }
    public LocalEmbed? Embed { get; init; }
}