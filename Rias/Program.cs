using System;
using System.IO;
using System.Threading.Tasks;
using Rias.Core;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Rias
{
    public class Program
    {
        private static readonly string LogPath = Path.Combine(Environment.CurrentDirectory, "logs/rias-.log");

        public static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Verbose()
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.Console(theme: SystemConsoleTheme.Literate)
                .WriteTo.Async(x => x.File(LogPath,  shared: true, rollingInterval: RollingInterval.Day))
                .CreateLogger();

            await new RiasBot().StartAsync();
            await Task.Delay(-1);
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject.ToString());
            Log.CloseAndFlush();
        }
    }
}