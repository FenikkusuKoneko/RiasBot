using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;

namespace Rias.Core.Services
{
    public class AdministrationService : RiasService
    {
        private readonly HttpClient _http;

        public AdministrationService(IServiceProvider services) : base(services)
        {
            _http = services.GetRequiredService<HttpClient>();
        }

        public async Task<bool> SetGreetAsync(SocketTextChannel channel)
        {
            var guild = channel.Guild;
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);

            if (guildDb is null)
                return false;
            
            var webhook = guildDb.GreetWebhookId > 0 ? await guild.GetWebhookAsync(guildDb.GreetWebhookId) : null;
            guildDb.GreetNotification = !guildDb.GreetNotification;
            if (!guildDb.GreetNotification)
            {
                if (webhook != null)
                    await webhook.DeleteAsync();
                
                await db.SaveChangesAsync();
                return false;
            }
            
            if (webhook != null)
                await webhook.ModifyAsync(x => x.ChannelId = channel.Id);
            else
                webhook = await CreateWebhookAsync(channel);

            guildDb.GreetWebhookId = webhook.Id;
            await db.SaveChangesAsync();
            return true;
        }

        public string? GetGreetMessage(SocketGuild guild)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id)?.GreetMessage;
        }

        public async Task SetGreetMessageAsync(SocketGuild guild, string message)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                guildDb.GreetMessage = message;
            }
            else
            {
                var greetMsg = new Guilds
                {
                    GuildId = guild.Id,
                    GreetMessage = message
                };
                await db.AddAsync(greetMsg);
            }

            await db.SaveChangesAsync();
        }

        public async Task<bool> SetByeAsync(SocketTextChannel channel)
        {
            var guild = channel.Guild;
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);

            if (guildDb is null)
                return false;
            
            var webhook = guildDb.ByeWebhookId > 0 ? await guild.GetWebhookAsync(guildDb.ByeWebhookId) : null;
            guildDb.ByeNotification = !guildDb.ByeNotification;
            if (!guildDb.ByeNotification)
            {
                if (webhook != null)
                    await webhook.DeleteAsync();
                
                await db.SaveChangesAsync();
                return false;
            }

            if (webhook != null)
                await webhook.ModifyAsync(x => x.ChannelId = channel.Id);
            else
                webhook = await CreateWebhookAsync(channel);

            guildDb.ByeWebhookId = webhook.Id;
            await db.SaveChangesAsync();
            return true;
        }

        public string? GetByeMessage(SocketGuild guild)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id)?.ByeMessage;
        }

        public async Task SetByeMessageAsync(SocketGuild guild, string message)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                guildDb.ByeMessage = message;
            }
            else
            {
                var byeMsg = new Guilds
                {
                    GuildId = guild.Id,
                    ByeMessage = message
                };
                await db.AddAsync(byeMsg);
            }

            await db.SaveChangesAsync();
        }

        public async Task<bool> SetModLogAsync(SocketGuild guild, IMessageChannel channel)
        {
            var modlog = false;

            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                if (guildDb.ModLogChannelId != channel.Id)
                {
                    guildDb.ModLogChannelId = channel.Id;
                    modlog = true;
                }
                else
                {
                    guildDb.ModLogChannelId = 0;
                }
            }
            else
            {
                var newModLog = new Guilds
                {
                    GuildId = guild.Id,
                    ModLogChannelId = channel.Id
                };
                await db.AddAsync(newModLog);
            }

            await db.SaveChangesAsync();
            return modlog;
        }

        public ulong? GetModLogChannelId(SocketGuild guild)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id)?.ModLogChannelId;
        }

        private async Task<RestWebhook> CreateWebhookAsync(SocketTextChannel channel)
        {
            var currentUser = channel.Guild.CurrentUser;
            await using var stream = await _http.GetStreamAsync(currentUser.GetRealAvatarUrl());
            return await channel.CreateWebhookAsync(currentUser.Username, stream);
        }
    }
}