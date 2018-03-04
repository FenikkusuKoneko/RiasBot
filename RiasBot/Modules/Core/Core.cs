using RiasBot.Commons.Attributes;
using RiasBot.Services;
using Discord;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using unirest_net.http;
using RiasBot.Extensions;
using Discord.WebSocket;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using RiasBot.Commons.Patreon;
using DiscordBotsList.Api;

namespace RiasBot.Modules.Core
{
    public partial class Core : RiasModule
    {
        private readonly DiscordSocketClient _client;
        public readonly CommandHandler _ch;
        public readonly CommandService _service;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        public Core(DiscordSocketClient client, CommandHandler ch, CommandService service, DbService db, IBotCredentials creds)
        {
            _client = client;
            _ch = ch;
            _service = service;
            _db = db;
            _creds = creds;
        }

        [Command("dbl")]
        public async Task DBL()
        {
            AuthDiscordBotListApi dblApi = new AuthDiscordBotListApi(_creds.ClientId, _creds.DiscordBotsListApiKey);
            var dblSelfBot = await dblApi.GetMeAsync();
            var dblVoters = await dblSelfBot.GetVotersAsync(1);

            string dbl = null;
            foreach (var dblVoter in dblVoters)
            {
                dbl += $"{dblVoter.Username}#{dblVoter.Discriminator} ID: {dblVoter.Id}\n";
            }
            await ReplyAsync("Voters today:\n" + dbl);
        }

        [Command("patreon")]
        public async Task Patreon()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _creds.PatreonAccessToken);

                var url = $"https://www.patreon.com/api/oauth2/api/campaigns/1523534/pledges";
                var data = await http.GetStringAsync(url);
                File.WriteAllText(Environment.CurrentDirectory + "/patreon_campaign.txt", data);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task RunTask(int number)
        {
            var stw = new Stopwatch();
            stw.Start();

            var input = await GetUserInputAsync(Context.User.Id, Context.Channel.Id);
            input = input?.ToLowerInvariant().ToString();
            if (input != "y")
            {
                return;
            }
            stw.Stop();
            await ReplyAsync($"Task {number} has been cancelled {stw.Elapsed}");
        }

        public async Task<string> GetUserInputAsync(ulong userId, ulong channelId)
        {
            var userInputTask = new TaskCompletionSource<string>();
            var dsc = (DiscordSocketClient)Context.Client;
            try
            {
                dsc.MessageReceived += MessageReceived;

                if ((await Task.WhenAny(userInputTask.Task, Task.Delay(10000))) != userInputTask.Task)
                {
                    return null;
                }

                return await userInputTask.Task;
            }
            finally
            {
                dsc.MessageReceived -= MessageReceived;
            }

            Task MessageReceived(SocketMessage arg)
            {
                Task.Run(() =>
                {
                if (!(arg is SocketUserMessage userMsg) ||
                    !(userMsg.Channel is ITextChannel chan) ||
                    userMsg.Author.Id != userId ||
                    userMsg.Channel.Id != channelId)
                {
                    return Task.CompletedTask;
                }
                userInputTask.TrySetResult(arg.Content);
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }
    }
}
