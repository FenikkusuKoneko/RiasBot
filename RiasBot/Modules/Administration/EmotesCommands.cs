using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class EmotesCommands : RiasSubmodule
        {
            public EmotesCommands()
            {

            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageEmojis)]
            [RequireBotPermission(GuildPermission.ManageEmojis)]
            [RequireContext(ContextType.Guild)]
            public async Task AddEmote(string url, [Remainder]string name)
            {
                name = name.Replace(" ", "_");

                try
                {
                    using (var http = new HttpClient())
                    {
                        var res = await http.GetStreamAsync(new Uri(url)).ConfigureAwait(false);

                        var ms = new MemoryStream();
                        await res.CopyToAsync(ms);
                        ms.Position = 0;

                        if (ms.Length / 1024 <= 256) //in KB
                        {
                            var emoteImage = new Image(ms);
                            await Context.Guild.CreateEmoteAsync(name, emoteImage).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} emote {Format.Bold(name)} was created successfully.").ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the image is bigger than 256 KB.").ConfigureAwait(false);
                        }
                    }
                }
                catch
                {
                    var staticEmotes = new List<IEmote>();
                    var animatedEmotes = new List<IEmote>();
                    var emotes = Context.Guild.Emotes;
                    foreach (var emote in emotes)
                    {
                        if (emote.Animated)
                            animatedEmotes.Add(emote);
                        else
                            staticEmotes.Add(emote);
                    }
                    if (staticEmotes.Count == 50)
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the server already has the limit of 50 non-animated emotes.");
                    else if (animatedEmotes.Count == 50)
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the server already has the limit of 50 animated emotes.");
                    else
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the image or the URL are not good.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageEmojis)]
            [RequireBotPermission(GuildPermission.ManageEmojis)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteEmote([Remainder]string name)
            {
                try
                {
                    var emote = Context.Guild.Emotes.Where(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
                    if (emote is null)
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the emote.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Guild.DeleteEmoteAsync(emote).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} emote {Format.Bold(emote.Name)} was deleted successfully.");
                    }
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't delete the emote.");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageEmojis)]
            [RequireBotPermission(GuildPermission.ManageEmojis)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameEmote([Remainder]string name)
            {
                var emotes = name.Split("->");
                string oldName = emotes[0].TrimEnd().Replace(" ", "_");
                string newName = emotes[1].TrimStart().Replace(" ", "_");
                try
                {
                    var emote = Context.Guild.Emotes.Where(x => x.Name.ToLowerInvariant() == oldName.ToLowerInvariant()).FirstOrDefault();
                    if (emote is null)
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the emote.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Guild.ModifyEmoteAsync(emote, x => x.Name = newName).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} emote {Format.Bold(emote.Name)} was renamed to {Format.Bold(newName)} successfully.");
                    }
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't rename the emote.");
                }
            }
        }
    }
}
