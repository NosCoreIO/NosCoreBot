using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NosCore.ParserInputGenerator.Downloader;
using NosCore.ParserInputGenerator.Extractor;
using NosCoreBot.Services;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NosCoreBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                    {
                        AlwaysDownloadUsers = true,
                        GatewayIntents = GatewayIntents.All
                    }))
                        .AddHttpClient()
                        .AddTransient<IExtractor, Extractor>()
                        .AddTransient<IClientDownloader, ClientDownloader>()
                        .AddSingleton<CommandService>()
                        .AddSingleton<CommandHandlingService>()
                        .AddSingleton<TimeHandlingService>()
                        .AddSingleton<HttpClient>()
                        .BuildServiceProvider();
                    services.AddHostedService<Worker>();
                });

        }
    }

    public class Worker : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmservice;
        private readonly CommandHandlingService _chservice;
        private readonly TimeHandlingService _thservice;

        public Worker(DiscordSocketClient client, CommandService cmservice, CommandHandlingService chservice,
            TimeHandlingService thservice)
        {
            _client = client;
            _cmservice = cmservice;
            _chservice = chservice;
            _thservice = thservice;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.Log += LogAsync;
            _cmservice.Log += LogAsync;

            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("token"));
            await _client.StartAsync();

            await _chservice.InitializeAsync();
            await _thservice.UploadInputFilesAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }
    }
}
