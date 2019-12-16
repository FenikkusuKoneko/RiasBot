using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;

namespace Rias.Core.Services
{
    public class InfoService : RiasService
    {
        public double CpuUsage { get; private set; }
        public double RamUsage { get; private set; }

        private readonly int _currentProcessId;

        public InfoService(IServiceProvider services) : base(services)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return;
            
            using var currentProcess = Process.GetCurrentProcess();
            _currentProcessId = currentProcess.Id;

            _ = Task.Run(StartMetricsAsync);
            Log.Information("Metrics started");
        }

        private Task StartMetricsAsync()
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"top\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };

            process.OutputDataReceived += OutputDataReceivedAsync;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            return Task.CompletedTask;
        }

        private void OutputDataReceivedAsync(object sendingProcess, DataReceivedEventArgs args)
        {
            var output = (args.Data).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var processInfo = output.FirstOrDefault(x => x.Contains(_currentProcessId.ToString()));
            if (string.IsNullOrEmpty(processInfo))
                return;
            
            var processStats = processInfo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            double.TryParse(processStats[8], out var cpuUsage);
            double.TryParse(processStats[9], out var ramUsage);

            if (cpuUsage > 0)
                CpuUsage = cpuUsage;

            if (ramUsage > 0)
                RamUsage = ramUsage;
        }
    }
}