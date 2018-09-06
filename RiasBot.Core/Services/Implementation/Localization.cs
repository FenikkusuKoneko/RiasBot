using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace RiasBot.Services.Implementation
{
    public class Localization
    {
        private static readonly Dictionary<string, CommandData> _commandData;
        
        static Localization()
        {
            _commandData = JsonConvert.DeserializeObject<Dictionary<string, CommandData>>(
                File.ReadAllText("./_strings/commands.json"));
        }

        private Localization() { }

        public static CommandData LoadCommand(string key)
        {
            key = key.Replace("async", "", StringComparison.InvariantCultureIgnoreCase);
            _commandData.TryGetValue(key, out var toReturn);

            if (toReturn == null)
                return new CommandData
                {
                    Command = key,
                    Description = key,
                    Remarks = new[] { key },
                };

            return toReturn;
        }
    }

    public class CommandData
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public string[] Remarks { get; set; }
    }
}
