using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Rias.Core.Attributes;
using Serilog;
using Qmmands;
using Rias.Core.Implementation;
using Serilog.Events;

namespace Rias.Core.Services
{
    public class LoggingService : RiasService
    {
        [Inject] private readonly DiscordShardedClient _client;
        [Inject] private readonly CommandService _commandService;

        public LoggingService(IServiceProvider services) : base(services)
        {
            _client.Log += DiscordLogAsync;
            _commandService.CommandExecuted += CommandExecutedAsync;
            _commandService.CommandErrored += CommandErroredAsync;
        }

        private Task DiscordLogAsync(LogMessage msg)
        {
            LogEventLevel logEventLevel;
            switch (msg.Severity)
            {
                case LogSeverity.Verbose:
                    logEventLevel = LogEventLevel.Verbose;
                    break;
                case LogSeverity.Info:
                    logEventLevel = LogEventLevel.Information;
                    break;
                case LogSeverity.Debug:
                    logEventLevel = LogEventLevel.Debug;
                    break;
                case LogSeverity.Warning:
                    logEventLevel = LogEventLevel.Warning;
                    break;
                case LogSeverity.Error:
                    logEventLevel = LogEventLevel.Error;
                    break;
                case LogSeverity.Critical:
                    logEventLevel = LogEventLevel.Fatal;
                    break;
                default:
                    logEventLevel = LogEventLevel.Verbose;
                    break;
            }

            Log.Logger.Write(logEventLevel, $"{msg.Source}: {msg.Exception?.ToString() ?? msg.Message}");

            return Task.CompletedTask;
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

        private Task CommandErroredAsync(CommandErroredEventArgs args)
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