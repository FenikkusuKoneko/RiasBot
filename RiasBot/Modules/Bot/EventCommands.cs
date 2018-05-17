using Discord;
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
            public async Task Event(int timeout, int reward, int maximum, int differencePerUser, bool onlyNumbers, bool botStarts, [Remainder]string message)
            {
                await Context.Channel.SendConfirmationEmbed(message).ConfigureAwait(false);
                await Task.Factory.StartNew(() => _service.CounterSetup(Context.Channel, reward, maximum, differencePerUser, onlyNumbers, botStarts)).ConfigureAwait(false);
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Event(int timeout, int reward, [Remainder]string message)
            {
                IUserMessage userMessage = null;
                userMessage = await Context.Channel.SendConfirmationEmbed(message).ConfigureAwait(false);

                await Task.Factory.StartNew(() => _service.Hearts(userMessage, timeout, reward)).ConfigureAwait(false);
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
