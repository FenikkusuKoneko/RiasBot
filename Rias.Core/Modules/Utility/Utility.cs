using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Utility
{
    [Name("Utility")]
    public class Utility : RiasModule
    {
        public InteractiveService InteractiveService { get; set; }

        [Command("test")]
        public async Task Test()
        {
            var pages = new List<EmbedBuilder>();
            for (var i = 0; i < 5; i++)
            {
                pages.Add(new EmbedBuilder
                {
                    Color = new Color(0xd40000),
                    Title = "Paginator",
                    Description = $"Page #{i+1}"
                });
            }

            await InteractiveService.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage
            {
                Pages = pages,
                Config = new PaginatorConfig
                {
                    UseStop = true,
                    StopOptions = StopOptions.SourceUser,
                    UseJump = true
                }
            });
        }
    }
}