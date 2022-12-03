using Disqord;

namespace Rias.Services.Responses.Administration;

public class SetByeMessageResponse
{
    public bool IsByeEnabled { get; init; }
    public IGuildChannel? Channel { get; init; }
}