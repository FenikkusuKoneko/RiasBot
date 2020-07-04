using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Implementation;
using Serilog;
using Serilog.Events;

namespace Rias.Core.Services
{
    [AutoStart]
    public class LoggingService : RiasService
    {
        public LoggingService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            var commandService = serviceProvider.GetRequiredService<CommandService>();
            
            RiasBot.Logger.MessageLogged += DisqordLog;
            commandService.CommandExecuted += CommandExecutedAsync;
            commandService.CommandExecutionFailed += CommandExecutionFailedAsync;
        }
        
        private void DisqordLog(object? sender, MessageLoggedEventArgs args)
        {
            var logEventLevel = args.Severity switch
            {
                LogMessageSeverity.Trace => LogEventLevel.Verbose,
                LogMessageSeverity.Information => LogEventLevel.Information,
                LogMessageSeverity.Debug => LogEventLevel.Debug,
                LogMessageSeverity.Warning => LogEventLevel.Warning,
                LogMessageSeverity.Error => LogEventLevel.Error,
                LogMessageSeverity.Critical => LogEventLevel.Fatal,
                _ => LogEventLevel.Verbose
            };

            if (args.Message != null && (Regex.IsMatch(args.Message, @"Guild.*became available") || args.Message.Contains("MessageUpdated")))
                return;
            
            Log.Logger.Write(logEventLevel, $"{args.Source}: {args.Exception?.ToString() ?? args.Message}");
        }

        private Task CommandExecutedAsync(CommandExecutedEventArgs args)
        {
            var context = (RiasCommandContext) args.Context;
            var command = context.Command;

            Log.Logger.Information($"[Command] \"{command.Name}\"\n" +
                            $"\t\t[Arguments] \"{string.Join(" ", context.Arguments)}\"\n" +
                            $"\t\t[User] \"{context.User}\" ({context.User.Id})\n" +
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
                            $"\t\t[User] \"{context.User}\" ({context.User.Id})\n" +
                            $"\t\t[Channel] \"{context.Channel.Name}\" ({context.Channel.Id})\n" +
                            $"\t\t[Guild] \"{context.Guild?.Name ?? "DM"}\" ({context.Guild?.Id ?? 0})\n" +
                            $"\t\t[Error Reason] {result.Reason}\n" +
                            $"\t\t[Error Exception] {result.Exception}");

            return Task.CompletedTask;
        }
    }
}