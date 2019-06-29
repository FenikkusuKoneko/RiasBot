using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Implementation;
using CommandService = Qmmands.CommandService;

namespace Rias.Core.Services
{
    public class CommandHandlerService : RiasService
    {
        [Inject] private readonly DiscordShardedClient _client;
        [Inject] private readonly CommandService _service;
        [Inject] private readonly Credentials _creds;
        [Inject] private readonly Translations _tr;
        [Inject] private readonly IServiceProvider _services;

        private readonly ConcurrentDictionary<string, CommandData> _commandData;

        public CommandHandlerService(IServiceProvider services) : base(services)
        {
            _services = services;
            
            _commandData = JsonConvert.DeserializeObject<ConcurrentDictionary<string, CommandData>>(
                File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "data/commands_strings.json")));
            
            LoadCommands();
            
            _client.MessageReceived += MessageReceivedAsync;
        }

        private void LoadCommands()
        {
            var assembly = Assembly.GetAssembly(typeof(Rias));
            _service.AddModules(assembly, null, module =>
            {
                foreach (var command in module.Commands)
                {
                    var name = command.Aliases.FirstOrDefault();
                    if (string.IsNullOrEmpty(name)) continue;

                    if (!_commandData.TryGetValue(name, out var commandData)) continue;

                    command.Name = name;
                    if (!string.IsNullOrEmpty(commandData.Aliases))
                    {
                        foreach (var alias in commandData.Aliases.Split(" "))
                        {
                            command.AddAlias(alias);
                        }
                    }

                    command.Description = commandData.Description;
                    command.Remarks = string.Join("\n", commandData.Remarks);
                }
            });
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage)) return;
            if (userMessage.Author.IsBot) return;
            
            if (!(CommandUtilities.HasPrefix(userMessage.Content, _creds.Prefix, out var output)
            || CommandUtilities.HasPrefix(userMessage.Content, $"{_client.CurrentUser.Username} ", StringComparison.InvariantCultureIgnoreCase, out output)
            || CommandUtilities.HasPrefix(userMessage.Content, $"{_client.CurrentUser.Mention} ", StringComparison.InvariantCultureIgnoreCase, out output)))
                return;

            var context = new RiasCommandContext(_client, userMessage);
            var result = await _service.ExecuteAsync(output, context, _services);

            if (result is ChecksFailedResult failedResult)
            {
                _ = Task.Run(async () => await SendErrorResultMessageAsync(context, userMessage, failedResult));
            }
        }
        
        private async Task SendErrorResultMessageAsync(RiasCommandContext context, SocketUserMessage userMessage, ChecksFailedResult result)
        {
            var embed = new EmbedBuilder().WithColor(RiasUtils.ErrorColor)
                .WithTitle(_tr.GetText(context.Guild.Id, null, "#service_command_not_executed"));

            var failedChecks = result.FailedChecks;
            var description = _tr.GetText(context.Guild.Id, null, "#administration_reason");
            if (failedChecks.Count > 1)
                description = _tr.GetText(context.Guild.Id, null, "#administration_reasons");

            embed.WithDescription($"**{description}**:\n{string.Join("\n", failedChecks.Select(x => _tr.GetText(context.Guild.Id, null, x.Result.Reason)))}");
            var timeoutMessage = await userMessage.Channel.SendMessageAsync(embed: embed.Build());
            await Task.Delay(10000);
            await timeoutMessage.DeleteAsync();
        }

        private class CommandData
        {
            public string Aliases { get; set; }
            public string Description { get; set; }
            public string[] Remarks { get; set; }
        }
    }
}