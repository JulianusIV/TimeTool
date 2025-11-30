using DSharpPlus;
using DSharpPlus.Commands;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;

namespace TimeTool
{
    public class Program
    {
        public static async Task Main()
        {
#if DEBUG
            foreach (var line in File.ReadAllLines("settings.env"))
                Environment.SetEnvironmentVariable(line[..line.IndexOf('=')], line[(line.IndexOf('=') + 1)..]);
#endif

            DiscordClient client = DiscordClientBuilder.CreateDefault(
                Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? throw new Exception("Please set DISCORD_TOKEN EnvVar!"),
                DiscordIntents.None)
#if DEBUG
                .SetLogLevel(LogLevel.Debug)
#else
                .SetLogLevel(LogLevel.Error)
#endif
                .UseCommands((serviceProvider, extension) =>
                {
                    extension.AddCommands(Assembly.GetExecutingAssembly());
                },
                new CommandsConfiguration
                {
                    RegisterDefaultCommandProcessors = true,
                    UseDefaultCommandErrorHandler = true
                })
                .Build();

            await client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}