using Disqord;

namespace Rias.Services.Responses.Administration;

public class SetByeMessageResponse
{
    public bool ByeEnabled { get; init; }
    public IGuildChannel? Channel { get; init; }
}