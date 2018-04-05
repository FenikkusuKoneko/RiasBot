using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Bot.Services;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Bot
{
    public partial class Bot
    {
        public class EventCommands : RiasSubmodule<EventService>
        {
            private readonly CommandHandler _ch;
            private readonly DbService _db;

            public EventCommands(CommandHandler ch, CommandService service, DbService db)
            {
                _ch = ch;
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Event(string game, int reward, int maximum = 0, int differencePerUser = 0, bool onlyNumbers = false, bool botStarts = false, [Remainder]string message = null)
            {
                game = game.ToLowerInvariant();
                if (!String.IsNullOrEmpty(message))
                    await Context.Channel.SendConfirmationEmbed(message).ConfigureAwait(false);
                switch (game)
                {
                    case "counter":
                        await _service.CounterSetup(Context.Channel, reward, maximum, differencePerUser, onlyNumbers, botStarts).ConfigureAwait(false);
                        break;
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task StopEvent()
            {
                _service.gameStarted = false;
                await Context.Channel.SendConfirmationEmbed("Event stopped!").ConfigureAwait(false);
            }
        }
    }
}
