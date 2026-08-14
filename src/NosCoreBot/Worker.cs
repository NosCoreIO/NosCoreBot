//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using NosCoreBot.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCoreBot;

public class Worker : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _cmservice;
    private readonly CommandHandlingService _chservice;
    private readonly TimeHandlingService _thservice;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(DiscordSocketClient client, CommandService cmservice, CommandHandlingService chservice,
        TimeHandlingService thservice, IHostApplicationLifetime lifetime)
    {
        _client = client;
        _cmservice = cmservice;
        _chservice = chservice;
        _thservice = thservice;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += LogAsync;
        _cmservice.Log += LogAsync;

        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.Ready += () =>
        {
            ready.TrySetResult();
            return Task.CompletedTask;
        };

        // Tokens should be considered secret data, and never hard-coded.
        await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("token"));
        await _client.StartAsync();
        await ready.Task.WaitAsync(TimeSpan.FromMinutes(2), stoppingToken);

        if (Environment.GetEnvironmentVariable("RUN_ONCE") == "true")
        {
            try
            {
                await _thservice.UploadInputFilesAsync();
            }
            finally
            {
                await _client.StopAsync();
                _lifetime.StopApplication();
            }
            return;
        }

        await _chservice.InitializeAsync();
        await _thservice.UploadInputFilesAsync();

        await Task.Delay(-1, stoppingToken);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());

        return Task.CompletedTask;
    }
}