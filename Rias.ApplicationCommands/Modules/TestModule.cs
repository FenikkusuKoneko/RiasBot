using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Rias.ApplicationCommands.Modules;

public class TestModule : DiscordApplicationGuildModuleBase
{
    [SlashCommand("echo")]
    public IResult Echo(string text)
    {
        return Response(text);
    }
}