using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Extensions;
using Rias.Implementation;
using Serilog;

namespace Rias.Services
{
    [AutoStart]
    public class LoggingService : RiasService
    {
        public LoggingService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            var commandService = serviceProvider.GetRequiredService<CommandService>();
            
            commandService.CommandExecuted += CommandExecutedAsync;
            commandService.CommandExecutionFailed += CommandExecutionFailedAsync;
        }

        private Task CommandExecutedAsync(CommandExecutedEventArgs args)
        {
            var context = (RiasCommandContext) args.Context;
            var command = context.Command;

            Log.Logger.Information($"[Command] \"{command.Name}\"\n" +
                            $"\t\t[Arguments] \"{string.Join(" ", context.Arguments)}\"\n" +
                            $"\t\t[User] \"{context.User.FullName()}\" ({context.User.Id})\n" +
                            $"\t\t[Channel] \"{context.Channel.Name}\" ({context.Channel.Id})\n" +
                            $"\t\t[Guild] \"{context.Guild?.Name ?? "DM"}\" ({context.Guild?.Id ?? 0})");

            return Task.CompletedTask;
        }

        private Task CommandExecutionFailedAsync(CommandExecutionFailedEventArgs args)
        {
            var context = (RiasCommandContext) args.Context;
            var command = context.Command;
            var result = args.Result;

            Log.Error($"[Command] \"{command.Name}\"\n" +
                            $"\t\t[Arguments] \"{context.RawArguments}\"\n" +
                            $"\t\t[User] \"{context.User.FullName()}\" ({context.User.Id})\n" +
                            $"\t\t[Channel] \"{context.Channel.Name}\" ({context.Channel.Id})\n" +
                            $"\t\t[Guild] \"{context.Guild?.Name ?? "DM"}\" ({context.Guild?.Id ?? 0})\n" +
                            $"\t\t[Error Reason] {result.Reason}\n" +
                            $"\t\t[Error Exception] {result.Exception}");

            return Task.CompletedTask;
        }
    }
}