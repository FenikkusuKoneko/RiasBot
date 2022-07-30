using Disqord.Bot.Commands.Text;
using Qmmands;
using Qmmands.Text;

namespace Rias.TextCommands.Modules;

public class TestModule : DiscordTextGuildModuleBase
{
    [TextCommand("echo")]
    public IResult Echo(string text)
    {
        return Response(text);
    }
}