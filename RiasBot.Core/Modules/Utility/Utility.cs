using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database;
using RiasBot.Services.Database.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ImageMagick;
using UnitsNet;

namespace RiasBot.Modules.Utility
{
    public partial class Utility : RiasModule
    {
        public readonly CommandHandler _ch;
        public readonly CommandService _service;
        public readonly DbService _db;

        public Utility(CommandHandler ch, CommandService service, DbService db)
        {
            _ch = ch;
            _service = service;
            _db = db;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Prefix([Remainder]string newPrefix = null)
        {
            var user = (IGuildUser)Context.User;
            if (newPrefix is null)
            {
                await Context.Channel.SendConfirmationEmbed($"{user.Mention} the prefix on this server is {Format.Bold(_ch.Prefix)}").ConfigureAwait(false);
            }
            else if (user.GuildPermissions.Administrator)
            {
                var oldPrefix = _ch.Prefix;

                using (var db = _db.GetDbContext())
                {
                    var guild = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();

                    try
                    {
                        guild.Prefix = newPrefix;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        var prefix = new GuildConfig { GuildId = Context.Guild.Id, Prefix = newPrefix };
                        await db.Guilds.AddAsync(prefix).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                await Context.Channel.SendConfirmationEmbed($"{user.Mention} the prefix on this server was changed from {Format.Bold(oldPrefix)} to {Format.Bold(newPrefix)}").ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendConfirmationEmbed($"{user.Mention} you don't have {Format.Bold("Administration")} permission").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Invite()
        {
            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} invite me on your server: [invite]({(RiasBot.Invite)})");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Donate()
        {
            await Context.Channel.SendConfirmationEmbed($"Support me! Support this project on [Patreon](https://www.patreon.com/riasbot).\n" +
                $"For every dollar donated you will receive 1000 {RiasBot.Currency}.");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Ping()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            stopwatch.Stop();
            await Context.Channel.SendConfirmationEmbed(":ping_pong:" + stopwatch.ElapsedMilliseconds + "ms").ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Choose([Remainder]string list)
        {
            var choices = list.Split(new Char[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            var choice = rnd.Next(choices.Length);
            await Context.Channel.SendConfirmationEmbed($"I chose: {Format.Bold(choices[choice].Trim())}");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task ConvertList()
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithTitle($"All categories for converter. Type {_ch.Prefix}convertlist <category> to get the units from a category");
            var unitCategories = "";
            var quantityTypes = Enum.GetValues(typeof(QuantityType)).Cast<QuantityType>().Skip(1).ToArray();

            var index = 0;
            foreach (var quantity in quantityTypes)
            {
                var type = Assembly.Load("UnitsNet").GetTypes().First(t => t.Name == quantity.ToString());
                
                if (unitCategories.Length <= 1024)
                {
                    unitCategories += $"#{index+1} {Format.Bold(quantity.ToString())}\n";
                }
                else
                {
                    embed.WithDescription(unitCategories);
                    await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    unitCategories = $"#{index+1} {Format.Bold(quantity.ToString())}\n";
                }
                index++;
            }
            embed.WithDescription(unitCategories);
            await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task ConvertList(string quantityIndex)
        {
            var quantityType = Enum.GetValues(typeof(QuantityType)).Cast<QuantityType>().Where(x => x.ToString().ToLowerInvariant() == quantityIndex.ToLowerInvariant()).FirstOrDefault();
            if (quantityType == QuantityType.Undefined)
            {
                return;
            }
            var type = Assembly.Load("UnitsNet").GetTypes().First(t => t.Name == quantityType.ToString());
            var method = type.GetMethod("get_Units");
            var unitsEnum = (Array)method.Invoke(null, null);

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithTitle($"All units for {type.Name}");

            var units = new string[unitsEnum.Length];
            var index = 0;
            foreach (var unit in unitsEnum)
            {
                if (unit.ToString() != "Undefined")
                {
                    units[index] = unit.ToString();
                    index++;
                }
            }
            embed.WithDescription(String.Join("\n", units));
            await Context.Channel.SendMessageAsync("", embed: embed.Build());
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Converter(string category, string from, string to, double value)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            if (!UnitConverter.TryConvertByName(value, ToTitleCase(category), ToTitleCase(from), ToTitleCase(to), out var result))
            {
                if (UnitConverter.TryConvertByAbbreviation(value, ToTitleCase(category), from.ToLower(), to.ToLower(), out result))
                {
                    embed.AddField("From", ToTitleCase(from), true).AddField("To", ToTitleCase(to), true);
                    embed.AddField("Result", result);
                    await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("Invalid units").ConfigureAwait(false);
                }
            }
            else
            {
                embed.AddField("From", ToTitleCase(from), true).AddField("To", ToTitleCase(to), true);
                embed.AddField("Result", result);
                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        public async Task Color([Remainder]string color)
        {
            color = color.Replace("#", "");
            if (int.TryParse(color.Substring(0, 2), NumberStyles.HexNumber, null, out var redColor) &&
                int.TryParse(color.Substring(2, 2), NumberStyles.HexNumber, null, out var greenColor) &&
                int.TryParse(color.Substring(4, 2), NumberStyles.HexNumber, null, out var blueColor))
            {
                var red = Convert.ToByte(redColor);
                var green = Convert.ToByte(greenColor);
                var blue = Convert.ToByte(blueColor);

                using (var img = new MagickImage(MagickColor.FromRgb(red, green, blue), 50, 50))
                {
                    var imageStream = new MemoryStream();
                    img.Write(imageStream, MagickFormat.Png);
                    imageStream.Position = 0;
                    await Context.Channel.SendFileAsync(imageStream, $"#{color}.png");
                }
            }
            else
            {
                try
                {
                    var magickColor = new MagickColor(color.Replace(" ", ""));
                    using (var img = new MagickImage(magickColor, 50, 50))
                    {
                        var imageStream = new MemoryStream();
                        img.Write(imageStream, MagickFormat.Png);
                        imageStream.Position = 0;
                        await Context.Channel.SendFileAsync(imageStream, $"#{color}.png");
                    }
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the color is not a valid hex color.").ConfigureAwait(false);
                }
            }
        }

        private static string ToTitleCase(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }
    }
}
