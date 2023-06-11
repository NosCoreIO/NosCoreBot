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