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
        public LoggingService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            var commandService = serviceProvider.GetRequiredService<CommandService>();
            
            commandService.CommandExecuted += CommandExecutedAsync;
            commandService.CommandExecutionFailed += CommandExecutionFailedAsync;
        }

        private Task CommandExecutedAsync(CommandExecutedEventArgs args)
        {
            var context = (RiasCommandContext) args.Context;
            var command = context.Command;
            
            Log.Information("{@Message}", new
            {
                Command = command.Name,
                Arguments = context.RawArguments,
                User = $"{context.User.FullName()} ({context.User.Id})",
                Channel = $"{context.Channel.Name} ({context.Channel.Id})",
                Guild = $"{context.Guild?.Name ?? "DM"} ({context.Guild?.Id ?? 0})"
            });

            return Task.CompletedTask;
        }

        private Task CommandExecutionFailedAsync(CommandExecutionFailedEventArgs args)
        {
            var context = (RiasCommandContext) args.Context;
            var command = context.Command;
            var result = args.Result;

            Log.Error(result.Exception, "{@Message}", new
            {
                Command = command.Name,
                Arguments = context.RawArguments,
                User = $"{context.User.FullName()} ({context.User.Id})",
                Channel = $"{context.Channel.Name} ({context.Channel.Id})",
                Guild = $"{context.Guild?.Name ?? "DM"} ({context.Guild?.Id ?? 0})",
                ErrorReason = result.FailureReason
            });

            return Task.CompletedTask;
        }
    }
}