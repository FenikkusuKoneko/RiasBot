using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Newtonsoft.Json;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Implementation;
using RiasBot.Extensions;

namespace Rias.Core.Services
{
    public class CommandHandlerService : RiasService
    {
        [Inject] private readonly DiscordShardedClient _client;
        [Inject] private readonly CommandService _service;
        [Inject] private readonly Credentials _creds;
        [Inject] private readonly Translations _tr;
        [Inject] private readonly IServiceProvider _services;
        
        public CommandHandlerService(IServiceProvider services) : base(services)
        {
            _services = services;

            LoadCommands();
            LoadTypeParsers();

            _client.MessageReceived += MessageReceivedAsync;
        }

        private void LoadCommands()
        {
            var commandDataJson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<CommandData>>>>(
                File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "data/commands_strings.json")));

            var assembly = Assembly.GetAssembly(typeof(Rias));
            _service.AddModules(assembly, null, module =>
            {
                if (!commandDataJson.TryGetValue(module.Name.ToLowerInvariant(), out var commandsDictionary)) return;
                if (!commandsDictionary.TryGetValue(module.Name.ToLowerInvariant(), out var commandsData)) return;

                SetupCommand(module, commandsData);

                foreach (var submodule in module.Submodules)
                {
                    if (!commandsDictionary.TryGetValue(submodule.Name.ToLowerInvariant(), out var submoduleCommandsData)) continue;
                    SetupCommand(submodule, submoduleCommandsData);
                }
            });
        }

        private void SetupCommand(ModuleBuilder module, List<CommandData> commandsData)
        {
            foreach (var command in module.Commands)
            {
                var name = command.Aliases.FirstOrDefault();
                if (string.IsNullOrEmpty(name)) continue;

                var commandData = commandsData
                    .Find(c => c.Aliases
                        .Split(" ")
                        .Any(a => string.Equals(a, name, StringComparison.InvariantCultureIgnoreCase)));
                if (commandData is null) continue;

                if (!string.IsNullOrEmpty(commandData.Aliases))
                {
                    foreach (var alias in commandData.Aliases.Split(" ").Skip(1))
                    {
                        command.AddAlias(alias);
                    }
                }

                command.Description = commandData.Description;
                command.Remarks = string.Join("\n", commandData.Remarks);
            }
        }

        private void LoadTypeParsers()
        {
            const string parserInterface = "ITypeParser";

            var typeParserInterface = _service.GetType().Assembly.GetTypes()
                .FirstOrDefault(x => x.Name == parserInterface)?.GetTypeInfo();

            if (typeParserInterface is null)
                throw new NullReferenceException(parserInterface);

            var assembly = Assembly.GetAssembly(typeof(Rias));
            var typeParsers = assembly.GetTypes()
                .Where(x => typeParserInterface.IsAssignableFrom(x)
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract)
                .ToArray();

            foreach (var typeParser in typeParsers)
            {
                var methodInfo = typeof(CommandService).GetMethods()
                    .First(m => m.Name == "AddTypeParser" && m.IsGenericMethodDefinition);

                var targetType = typeParser.BaseType?.GetGenericArguments().First();
                var genericMethodInfo = methodInfo.MakeGenericMethod(targetType);
                genericMethodInfo.Invoke(_service, new[] { Activator.CreateInstance(typeParser), false });
            }
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
            
            switch (result)
            {
                case ChecksFailedResult failedResult:
                    RunAsyncTask(SendErrorResultMessageAsync(context, userMessage, failedResult));
                    break;
                case CommandOnCooldownResult commandOnCooldownResult:
                    RunAsyncTask(SendCommandOnCooldownMessageAsync(context, commandOnCooldownResult));
                    break;
                case TypeParseFailedResult typeParseFailedResult:
                    RunAsyncTask(SendTypeParseFailedResult(context, typeParseFailedResult));
                    break;
            }
        }

        private async Task SendErrorResultMessageAsync(RiasCommandContext context, SocketUserMessage userMessage, ChecksFailedResult result)
        {
            var guildId = context.Guild?.Id;
            var embed = new EmbedBuilder().WithColor(RiasUtils.ErrorColor)
                .WithTitle(_tr.GetText(guildId, null, "#service_command_not_executed"));

            var failedChecks = result.FailedChecks;
            (CheckAttribute check, CheckResult checkResult) = (null, null);

            var description = _tr.GetText(guildId, null, "#common_reason");
            if (failedChecks.Count > 1)
            {
                description = _tr.GetText(guildId, null, "#common_reasons");
                (check, checkResult) = failedChecks.FirstOrDefault(x => x.Check is ContextAttribute);
            }

            embed.WithDescription(check is null
                ? $"**{description}**:\n{string.Join("\n", failedChecks.Select(x => x.Result.Reason))}"
                : $"**{description}**:\n{checkResult.Reason}");
            await userMessage.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task SendCommandOnCooldownMessageAsync(RiasCommandContext context, CommandOnCooldownResult result)
        {
            await context.Channel.SendErrorMessageAsync(_tr.GetText(context.Guild.Id, null, "#service_command_cooldown",
                result.Cooldowns.First().RetryAfter.Humanize(culture: CultureInfo.GetCultureInfo(_tr.GetGuildLocale(context.Guild.Id)))));
        }
        
        private async Task SendTypeParseFailedResult(RiasCommandContext context, TypeParseFailedResult result)
        {
            await context.Channel.SendErrorMessageAsync(_tr.GetText(context.Guild.Id, null, result.Reason));
        }

        private class CommandData
        {
            public string Aliases { get; set; }
            public string Description { get; set; }
            public string[] Remarks { get; set; }
        }
    }
}