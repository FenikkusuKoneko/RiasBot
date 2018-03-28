﻿using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class ServerCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly DbService _db;

            public ServerCommands(CommandHandler ch, CommandService service, DbService db)
            {
                _ch = ch;
                _service = service;
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageNicknames)]
            [RequireBotPermission(GuildPermission.ManageNicknames)]
            public async Task Nickname(IGuildUser user, [Remainder]string name = null)
            {
                if (user != null)
                {
                    if (Context.Guild.OwnerId != user.Id)
                    {
                        if (String.IsNullOrEmpty(name))
                        {
                            await user.ModifyAsync(x => x.Nickname = name).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {Format.Bold($"{user}'s nickname")} was changed to the default name {Format.Bold(user.ToString())}.").ConfigureAwait(false);
                        }
                        else
                        {
                            await user.ModifyAsync(x => x.Nickname = name).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {Format.Bold($"{user}'s nickname")} was changed to {Format.Bold(name)}.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you can't change the server's owner nickname.").ConfigureAwait(false);
                    }
                }
                else
                {
                    await ReplyAsync("I couldn't find the user.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            [RequireBotPermission(GuildPermission.ManageGuild)]
            public async Task SetGuildName([Remainder]string name)
            {
                await Context.Guild.ModifyAsync(x => x.Name = name);
                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} server's name changed to {Format.Bold(name)}.").ConfigureAwait(false);
            }
        }
    }
}