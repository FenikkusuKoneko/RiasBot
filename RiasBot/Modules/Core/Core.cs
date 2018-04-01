using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Core
{
    public class Core : RiasModule
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandHandler _ch;
        private readonly CommandService _service;
        private readonly DbService _db;

        public Core(DiscordSocketClient client, CommandHandler ch, CommandService service, DbService db)
        {
            _client = client;
            _ch = ch;
            _service = service;
            _db = db;
        }
    }
}
