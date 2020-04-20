using System;
using System.Collections.Generic;
using Serilog;

namespace Rias.Core.Services
{
    public class CooldownService : RiasService
    {
        public CooldownService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        private readonly HashSet<string> _cooldowns = new HashSet<string>();
        private readonly object _lock = new object();

        public void Add(string key)
        {
            lock (_lock)
            {
                _cooldowns.Add(key);
            }
            
            Log.Debug("Cooldown added");
        }

        public bool Has(string key)
        {
            lock (_lock)
            {
                return _cooldowns.Contains(key);
            }
        }

        public void Remove(string key)
        {
            lock (_lock)
            {
                _cooldowns.Remove(key);
            }
            
            Log.Debug("Cooldown removed");
        }

        public string GenerateKey(string name, ulong id, ulong? secondId = null)
            => secondId.HasValue ? $"{name}_{id}_{secondId}" : $"{name}_{id}";
    }
}