using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rias.Services.Commons
{
    public class CommandStatistics
    {
        private readonly object _lock = new object();
        private readonly CommandsAverage _commandsPerSecond = new CommandsAverage();
        private readonly CommandsAverage _commandsPerMinute = new CommandsAverage();
        private readonly CommandsAverage _commandsPerHour = new CommandsAverage();
        private readonly CommandsAverage _commandsPerDay = new CommandsAverage();
        
        private int _executedCommands;
        private int _attemptedCommands;

        public int ExecutedCommands => _executedCommands;

        public int AttemptedCommands => _attemptedCommands;

        public double CommandsPerSecondAverage => _commandsPerSecond.CommandsPerUnitAverage == 0
            ? _commandsPerSecond.CommandsPerUnit
            : _commandsPerSecond.CommandsPerUnitAverage;

        public double CommandsPerMinuteAverage => _commandsPerMinute.CommandsPerUnitAverage == 0
            ? _commandsPerMinute.CommandsPerUnit
            : _commandsPerMinute.CommandsPerUnitAverage;

        public double CommandsPerHourAverage => _commandsPerHour.CommandsPerUnitAverage == 0
            ? _commandsPerHour.CommandsPerUnit
            : _commandsPerHour.CommandsPerUnitAverage;

        public double CommandsPerDayAverage => _commandsPerDay.CommandsPerUnitAverage == 0
            ? _commandsPerDay.CommandsPerUnit
            : _commandsPerDay.CommandsPerUnitAverage;

        public void IncrementExecutedCommand()
            => Interlocked.Increment(ref _executedCommands);

        public void IncrementAttemptedCommand()
            => Interlocked.Increment(ref _attemptedCommands);

        public Task AddCommandTimestampAsync(DateTime timeStamp)
        {
            lock (_lock)
            {
                var ticks = timeStamp.Ticks;
                CalculateAverage(_commandsPerSecond, ticks, 10_000_000, 60);
                CalculateAverage(_commandsPerMinute, ticks, 600_000_000, 60);
                CalculateAverage(_commandsPerHour, ticks, 36_000_000_000, 24);
                CalculateAverage(_commandsPerDay, ticks, 864_000_000_000, 7);
            }
            
            return Task.CompletedTask;
        }

        private void CalculateAverage(CommandsAverage commandsAverage, long ticks, long unitTicks, double period)
        { 
            if (commandsAverage.FirstCommandTicks == 0 || ticks - commandsAverage.FirstCommandPeriodTicks >= unitTicks * period)
            {
                commandsAverage.FirstCommandTicks = ticks;
                commandsAverage.FirstCommandPeriodTicks = ticks;
                commandsAverage.CommandsPerUnit = 0;
                commandsAverage.CommandsPerUnitAverage = 0;
                commandsAverage.Counter = 0;
            }

            var commandUnitTicks = ticks - commandsAverage.FirstCommandTicks;
            if (commandUnitTicks < unitTicks)
            {
                commandsAverage.CommandsPerUnit++;
            }
            else
            {
                var units = commandUnitTicks / unitTicks;
                if (units > 1)
                {
                    for (var i = 0; i < units - 1; i++)
                        commandsAverage.CommandsPerUnitAverage = commandsAverage.Counter++ * commandsAverage.CommandsPerUnitAverage / commandsAverage.Counter;
                }
                
                commandsAverage.FirstCommandTicks = ticks - (commandUnitTicks - units * unitTicks);
                commandsAverage.CommandsPerUnitAverage = (commandsAverage.CommandsPerUnit + commandsAverage.Counter++ * commandsAverage.CommandsPerUnitAverage) / commandsAverage.Counter;
                commandsAverage.CommandsPerUnit = 1;
            }
        }

        private class CommandsAverage
        {
            public long FirstCommandTicks { get; set; }
            public long FirstCommandPeriodTicks { get; set; }
            public int CommandsPerUnit { get; set; }
            public double CommandsPerUnitAverage { get; set; }
            public int Counter { get; set; }
        }
    }
}