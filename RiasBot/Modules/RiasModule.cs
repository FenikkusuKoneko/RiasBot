using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules
{
    public abstract class RiasModule : ModuleBase
    {
        public readonly string ModuleTypeName;
        public readonly string LowerModuleTypeName;

        protected RiasModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();
        }

        public async Task<string> GetUserInputAsync(ulong userId, ulong channelId, int timeout) //seconds
        {
            var userInputTask = new TaskCompletionSource<string>();
            var dsc = (DiscordShardedClient)Context.Client;
            try
            {
                dsc.MessageReceived += MessageReceived;

                if ((await Task.WhenAny(userInputTask.Task, Task.Delay(timeout))) != userInputTask.Task)
                {
                    return null;
                }

                return await userInputTask.Task;
            }
            finally
            {
                dsc.MessageReceived -= MessageReceived;
            }

            Task MessageReceived(SocketMessage arg)
            {
                var _ = Task.Run(() =>
                {
                    if (!(arg is SocketUserMessage userMsg) ||
                        !(userMsg.Channel is ITextChannel chan) ||
                        userMsg.Author.Id != userId ||
                        userMsg.Channel.Id != channelId)
                    {
                        return Task.CompletedTask;
                    }

                    if (userInputTask.TrySetResult(arg.Content))
                    {
                        userMsg.DeleteAsync(new RequestOptions
                        {
                            Timeout = 1000
                        });
                    }
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }
    }

    public abstract class RiasModule<TService> : RiasModule where TService : IRService
    {
        public TService _service { get; set; }

        public RiasModule(bool isTopLevel = true) : base(isTopLevel)
        {
        }
    }

    public abstract class RiasSubmodule : RiasModule
    {
        protected RiasSubmodule() : base(false) { }
    }

    public abstract class RiasSubmodule<TService> : RiasModule<TService> where TService : IRService
    {
        protected RiasSubmodule() : base(false)
        {
        }
    }
}
