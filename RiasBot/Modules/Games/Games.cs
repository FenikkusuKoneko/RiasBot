using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Games.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Games
{
    public partial class Games : RiasModule
    {
        public Games()
        {

        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Rps(string rps)
        {
            rps = rps?.ToLowerInvariant();
            string[] types = { "rock", "paper", "scissors" };
            int playerChoice = 0;

            switch(rps)
            {
                case "rock":
                    playerChoice = 1;
                    break;
                case "r":
                    playerChoice = 1;
                    break;
                case "paper":
                    playerChoice = 2;
                    break;
                case "p":
                    playerChoice = 2;
                    break;
                case "scissors":
                    playerChoice = 3;
                    break;
                case "s":
                    playerChoice = 3;
                    break;
            }
            if (playerChoice > 0)
            {
                var rnd = new Random((int)DateTime.UtcNow.Ticks);
                int botChoice = rnd.Next(1, 4);

                if (botChoice % 3 + 1 == playerChoice)
                    await Context.Channel.SendConfirmationEmbed($"I chose {Format.Bold(types[botChoice - 1])}, you won!").ConfigureAwait(false);
                else if (playerChoice % 3 + 1 == botChoice)
                    await Context.Channel.SendErrorEmbed($"I chose {Format.Bold(types[botChoice - 1])}, you lost!").ConfigureAwait(false);
                else
                {
                    var embed = new EmbedBuilder().WithColor(0xffff00);
                    embed.WithDescription($"{Format.Bold(types[botChoice - 1])}, draw!");
                    await ReplyAsync("", embed: embed.Build());
                }
            }
        }
    }
}
