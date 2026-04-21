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
}
