using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Configuration
{
    public partial class Configuration
    {
        public class CommandsCommands : RiasSubmodule
        {
            private readonly DbService _db;
            public CommandsCommands(DbService db)
            {
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(Discord.GuildPermission.Administrator)]
            public async Task DeleteCommandMessage()
            {
                using (var db = _db.GetDbContext())
                {
                    bool deleteCmdMsg = false;
                    var guildDb = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                    if (guildDb != null)
                    {
                        if (guildDb.DeleteCommandMessage)
                        {
                            deleteCmdMsg = guildDb.DeleteCommandMessage = false;
                        }
                        else
                        {
                            deleteCmdMsg = guildDb.DeleteCommandMessage = true;
                        }
                    }
                    else
                    {
                        var deleteCmdMsgDb = new GuildConfig { GuildId = Context.Guild.Id, DeleteCommandMessage = true };
                        await db.AddAsync(deleteCmdMsgDb).ConfigureAwait(false);
                        deleteCmdMsg = true;
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    if (deleteCmdMsg)
                    {
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} automatically delete user's command message enabled.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} automatically delete user's command message disabled.").ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
