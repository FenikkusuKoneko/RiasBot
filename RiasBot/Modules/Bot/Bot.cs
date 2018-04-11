using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotsList.Api;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Searches.Services;
using RiasBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Modules.Bot
{
    public partial class Bot : RiasModule
    {
        private readonly CommandHandler _ch;
        private readonly CommandService _service;
        private readonly IServiceProvider _provider;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly BotService _botService;
        private readonly IBotCredentials _creds;

        private readonly CuteGirlsService _cuteGirlsService;

        public Bot(CommandHandler ch, CommandService service, IServiceProvider provider, DbService db, DiscordSocketClient client, BotService botService, IBotCredentials creds, CuteGirlsService cuteGirlsService)
        {
            _ch = ch;
            _service = service;
            _provider = provider;
            _db = db;
            _client = client;
            _botService = botService;
            _creds = creds;

            _cuteGirlsService = cuteGirlsService;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task LeaveGuild(ulong id)
        {
            var guild = await Context.Client.GetGuildAsync(id).ConfigureAwait(false);
            if (guild != null)
            {
                var usersGuild = await guild.GetUsersAsync();
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithDescription($"Leaving {Format.Bold(guild.Name)}");
                embed.AddField("Id", guild.Id, true).AddField("Users", usersGuild.Count, true);

                await ReplyAsync("", embed: embed.Build()).ConfigureAwait(false);

                await guild.LeaveAsync().ConfigureAwait(false);
            }
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireOwner]
        public async Task Die()
        {
            await Context.Channel.SendConfirmationEmbed("Shutting down...").ConfigureAwait(false);
            await Context.Client.StopAsync().ConfigureAwait(false);
            Environment.Exit(0);
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireOwner]
        public async Task Send(string id, [Remainder]string message)
        {
            IGuild guild;
            IUser user;
            ITextChannel channel;

            var embed = Extensions.Extensions.EmbedFromJson(message);

            if (id.Contains("|"))
            {
                try
                {
                    var ids = id.Split('|');
                    string guildId = ids[0];
                    string channelId = ids[1];

                    guild = await Context.Client.GetGuildAsync(Convert.ToUInt64(guildId)).ConfigureAwait(false);
                    channel = await guild.GetTextChannelAsync(Convert.ToUInt64(channelId)).ConfigureAwait(false);
                    if (embed is null)
                        await channel.SendMessageAsync(message).ConfigureAwait(false);
                    else
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed("Message sent!").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the guild or the channel");
                }
            }
            else
            {
                try
                {
                    user = await Context.Client.GetUserAsync(Convert.ToUInt64(id));
                    if (embed is null)
                        await user.SendMessageAsync(message).ConfigureAwait(false);
                    else
                        await user.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed("Message sent!").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the user").ConfigureAwait(false);
                }
            }
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireOwner]
        public async Task Edit(string channelMessage, [Remainder]string message)
        {
            try
            {
                var embed = Extensions.Extensions.EmbedFromJson(message);
                var ids = channelMessage.Split("|");
                UInt64.TryParse(ids[0], out ulong channelId);
                UInt64.TryParse(ids[1], out ulong messageId);


                var channel = await Context.Client.GetChannelAsync(channelId).ConfigureAwait(false);
                var msg = await ((ITextChannel)channel).GetMessageAsync(messageId).ConfigureAwait(false);

                if (embed is null)
                    await ((IUserMessage)msg).ModifyAsync(x => x.Content = message).ConfigureAwait(false);
                else
                    await ((IUserMessage)msg).ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed("Message edited!");
            }
            catch
            {
                await Context.Channel.SendConfirmationEmbed("I couldn't find the channel/message!");
            }
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireOwner]
        public async Task Dbl(int page = 1)
        {
            AuthDiscordBotListApi dblApi = new AuthDiscordBotListApi(_creds.ClientId, _creds.DiscordBotsListApiKey);
            var dblSelfBot = await dblApi.GetMeAsync().ConfigureAwait(false);
            var dbls = (await dblSelfBot.GetVotersAsync(1).ConfigureAwait(false)).GroupBy(x => x.Id).Select(y => y.FirstOrDefault()).ToList();

            string[] voters = new string[dbls.Count];
            int index = 0;
            foreach (var dbl in dbls)
            {
                voters[index] = $"#{index + 1} {dbl.Username}#{dbl.Discriminator} ({dbl.Id})";
                index++;
            }
            if (voters.Count() > 0)
                await Context.Channel.SendPaginated((DiscordSocketClient)Context.Client, "List of voters today", voters, 10, page - 1).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed("No voters today.").ConfigureAwait(false);
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireOwner]
        public async Task UpdateImages()
        {
            await _cuteGirlsService.UpdateNekos();
            await _cuteGirlsService.UpdateKitsunes();

            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} images updated.");
        }
    }
}
