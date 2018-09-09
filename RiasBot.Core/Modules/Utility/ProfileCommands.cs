using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Utility.Services;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class ProfileCommands : RiasSubmodule<ProfileService>
        {
            private readonly InteractiveService _is;
            private readonly DbService _db;

            public ProfileCommands(InteractiveService iss, DbService db)
            {
                _is = iss;
                _db = db;
            }
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [Ratelimit(1, 30, Measure.Seconds, applyPerGuild: true)]
            public async Task Profile([Remainder]IUser user = null)
            {
                user = user ?? Context.User;
                await Context.Channel.TriggerTypingAsync();

                var roles = new List<IRole>();
                var rolesIds = ((IGuildUser)user).RoleIds;
                foreach (var role in rolesIds)
                {
                    var r = Context.Guild.GetRole(role);
                    roles.Add(r);
                }
                var highestRole = roles.OrderByDescending(x => x.Position).Select(y => y).FirstOrDefault();

                using (var img = await _service.GenerateProfileImage((IGuildUser)user, highestRole))
                {
                    if (img != null)
                        await Context.Channel.SendFileAsync(img, $"{user.Id}_profile.png").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task BackgroundImage(string url)
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url is not a well formed uri string.").ConfigureAwait(false);
                    return;
                }
                if (!url.Contains("https"))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url must be https").ConfigureAwait(false);
                    return;

                }
                if (!url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg"))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url is not a direct link for a png, jpg or jpeg image.").ConfigureAwait(false);
                    return;
                }

                using (var db = _db.GetDbContext())
                using (var preview = await _service.GenerateBackgroundPreview((IGuildUser)Context.User, url))
                {
                    if (preview != null)
                        await Context.Channel.SendFileAsync(preview, $"{Context.User.Id}_preview.png").ConfigureAwait(false);
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync($"{Context.User} something went wrong! Check if the image is available or the url is a direct link.");
                        return;
                    }
                    await Context.Channel.SendConfirmationMessageAsync($"Do you want to set this background image? Price: 1000 {RiasBot.Currency}. Type `confirm` or `cancel`").ConfigureAwait(false);
                    var input = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                    if (input != null)
                    {
                        if (input.Content.ToLowerInvariant() != "confirm")
                        {
                            await Context.Channel.SendErrorMessageAsync("Canceled!").ConfigureAwait(false);
                            return;
                        }
                        var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                        var profileDb = db.Profile.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                        if (userDb != null)
                        {
                            if (userDb.Currency >= 1000)
                            {
                                userDb.Currency -= 1000;
                                if (profileDb != null)
                                {
                                    profileDb.BackgroundUrl = url;
                                }
                                else
                                {
                                    var image = new Profile { UserId = Context.User.Id, BackgroundUrl = url, BackgroundDim = 50 };
                                    await db.AddAsync(image).ConfigureAwait(false);
                                }
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} new background image set.").ConfigureAwait(false);
                                await db.SaveChangesAsync().ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have enough {RiasBot.Currency}.");
                            }
                        }
                        else
                        {
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have enough {RiasBot.Currency}.");
                        }
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task BackgroundDim(int dim)
            {
                if (dim >= 0 && dim <= 100)
                {
                    using (var db = _db.GetDbContext())
                    {
                        var profileDb = db.Profile.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                        if (profileDb != null)
                        {
                            profileDb.BackgroundDim = dim;
                        }
                        else
                        {
                            var dimDb = new Profile { UserId = Context.User.Id, BackgroundDim = dim };
                            await db.AddAsync(dimDb).ConfigureAwait(false);
                        }
                        await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} profile's background dim set to {dim}%.").ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task Biography([Remainder]string bio)
            {
                if (bio.Length <= 150)
                {
                    using (var db = _db.GetDbContext())
                    {
                        var profileDb = db.Profile.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                        if (profileDb != null)
                        {
                            profileDb.Bio = bio;
                        }
                        else
                        {
                            var bioDb = new Profile { UserId = Context.User.Id, BackgroundDim = 50, Bio = bio };
                            await db.AddAsync(bioDb).ConfigureAwait(false);
                        }
                        await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} new profile's bio set.").ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the bio's length must be less than 150 characters.");
                }
            }
        }
    }
}
