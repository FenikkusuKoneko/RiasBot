using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rias.Core.Implementation;
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

            var masterType = typeof(Core.Rias).Assembly
                .GetTypes()
                .FirstOrDefault(x => string.Equals(x.Name, "Master"));

            if (masterType != null)
                Activator.CreateInstance(masterType);
            
            var credentials = new Credentials();
            Log.Information(!credentials.IsDevelopment ? $"Initializing RiasBot version {Core.Rias.Version}" : $"Initializing development RiasBot version {Core.Rias.Version}");
            if (!VerifyCredentials(credentials))
                return;
            
            var databaseConnection = GetDatabaseConnection(credentials);
            if (databaseConnection is null)
                throw new NullReferenceException("The database connection is not set in credentials.json");
            
            await new Core.Rias(credentials, databaseConnection).RunAsync();
            await Task.Delay(-1);
        }
        
        private static bool VerifyCredentials(Credentials credentials)
        {
            if (string.IsNullOrEmpty(credentials.Token))
            {
                throw new NullReferenceException("You must set the token in credentials.json!");
            }

            if (!string.IsNullOrEmpty(credentials.Prefix)) return true;

            throw new NullReferenceException("You must set the default prefix in credentials.json!");
        }
        
        private static string? GetDatabaseConnection(Credentials credentials)
        {
            if (credentials.DatabaseConfig is null)
            {
                return null;
            }

            var connectionString = new StringBuilder();
            connectionString.Append("Host=").Append(credentials.DatabaseConfig.Host).Append(";");

            if (credentials.DatabaseConfig.Port > 0)
                connectionString.Append("Port=").Append(credentials.DatabaseConfig.Port).Append(";");

            connectionString.Append("Username=").Append(credentials.DatabaseConfig.Username).Append(";")
                .Append("Password=").Append(credentials.DatabaseConfig.Password).Append(";")
                .Append("Database=").Append(credentials.DatabaseConfig.Database).Append(";")
                .Append("ApplicationName=").Append(credentials.DatabaseConfig.ApplicationName);

            return connectionString.ToString();
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject.ToString());
            Log.CloseAndFlush();
        }
    }
}