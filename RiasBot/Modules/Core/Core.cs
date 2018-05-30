using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Core
{
    public partial class Core : RiasModule
    {
        private readonly DiscordShardedClient _client;
        public readonly CommandHandler _ch;
        public readonly DbService _db;

        public Core(DiscordShardedClient client, CommandHandler ch, DbService db)
        {
            _client = client;
            _ch = ch;
            _db = db;
        }
    }
}
