using Discord;
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
            private readonly DbService _db;

            public ProfileCommands(DbService db)
            {
                _db = db;
            }
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
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
                if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the url is not a well formed uri string.").ConfigureAwait(false);
                    return;
                }
                if (!url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg"))
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the url is not a direck link for a png, jpg or jpeg image.").ConfigureAwait(false);
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                    if (userDb != null)
                    {
                        if (userDb.Currency >= 1000)
                        {
                            var profileDb = db.Profile.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                            if (profileDb != null)
                            {
                                profileDb.BackgroundUrl = url;
                            }
                            else
                            {
                                var image = new Profile { UserId = Context.User.Id, BackgroundUrl = url, BackgroundDim = 50 };
                                await db.AddAsync(image).ConfigureAwait(false);
                            }
                            userDb.Currency -= 1000;
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} new profile's background image set.").ConfigureAwait(false);
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have enough {RiasBot.currency}.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have enough {RiasBot.currency}.").ConfigureAwait(false);
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
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} profile's background dim set to {dim}%.").ConfigureAwait(false);
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
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} new profile's bio set.").ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the bio's length must be less than 150 characters.");
                }
            }
        }
    }
}
