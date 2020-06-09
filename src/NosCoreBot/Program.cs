using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using NosCore.ParserInputGenerator.Downloader;
using NosCore.ParserInputGenerator.Extractor;
using NosCoreBot.Services;

namespace NosCoreBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program()
                .MainAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

        public async Task MainAsync()
        {
            await using var services = ConfigureServices();
            var client = services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            // Tokens should be considered secret data, and never hard-coded.
            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("token"));
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            await services.GetRequiredService<TimeHandlingService>().UploadInputFilesAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                }))
                .AddHttpClient()
                .AddTransient<IExtractor, Extractor>()
                .AddTransient<IClientDownloader, ClientDownloader>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<TimeHandlingService>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }
    }
}
