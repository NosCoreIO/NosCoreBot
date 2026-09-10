using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace NosCoreBot.Services;

public class SponsorSyncService
{
    public static readonly (string Name, int MinimumCents, Color Color)[] Tiers =
    {
        ("Legendary Supporter", 5000, Color.Gold),
        ("Gold Supporter", 1000, Color.Orange),
        ("Supporter", 300, Color.Blue)
    };

    public const string TopSponsorRole = "Top Sponsor";

    private readonly DiscordSocketClient _discord;
    private readonly SponsorService _sponsors;
    private readonly ILogger<SponsorSyncService> _logger;
    private readonly Timer _timer = new(TimeSpan.FromMinutes(15).TotalMilliseconds);

    public SponsorSyncService(DiscordSocketClient discord, SponsorService sponsors,
        ILogger<SponsorSyncService> logger)
    {
        _discord = discord;
        _sponsors = sponsors;
        _logger = logger;
        _timer.Elapsed += async (_, _) => await TickAsync();
    }

    public void Start()
    {
        _timer.Start();
    }

    private async Task TickAsync()
    {
        try
        {
            var reportHour = int.TryParse(Environment.GetEnvironmentVariable("SPONSOR_REPORT_HOUR"), out var hour)
                ? hour
                : 9;
            var state = await _sponsors.LoadStateAsync();
            if (DateTime.UtcNow.Hour != reportHour || state.LastReportUtc?.Date == DateTime.UtcNow.Date)
            {
                return;
            }

            await RunAsync(state);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Sponsor sync failed");
        }
    }

    public async Task RunAsync(SponsorState state)
    {
        state.Snapshot = await _sponsors.FetchAsync();
        state.LastReportUtc = DateTime.UtcNow;
        await _sponsors.SaveStateAsync(state);

        var guild = GetGuild();
        if (guild == null)
        {
            _logger.LogWarning("SPONSOR_GUILD_ID is unset or the bot is not in that guild");
            return;
        }

        await SyncRolesAsync(guild, state);

        if (ulong.TryParse(Environment.GetEnvironmentVariable("SPONSOR_CHANNEL_ID"), out var channelId)
            && guild.GetTextChannel(channelId) is { } channel)
        {
            await channel.SendMessageAsync(embed: BuildEmbed(state.Snapshot));
        }
        else
        {
            _logger.LogWarning("SPONSOR_CHANNEL_ID is unset or not a text channel in the guild");
        }
    }

    private SocketGuild GetGuild()
    {
        return ulong.TryParse(Environment.GetEnvironmentVariable("SPONSOR_GUILD_ID"), out var guildId)
            ? _discord.GetGuild(guildId)
            : null;
    }

    public static Embed BuildEmbed(IReadOnlyList<SponsorEntry> snapshot)
    {
        var builder = new EmbedBuilder()
            .WithTitle("Sponsor Leaderboard")
            .WithColor(Color.Gold)
            .WithFooter("Sponsors who chose to stay private are listed as Anonymous. "
                + "GitHub totals are estimated from tier and start date.")
            .WithCurrentTimestamp();

        if (snapshot.Count == 0)
        {
            builder.WithDescription("No sponsors yet. Be the first: https://github.com/sponsors/0Lucifer0");
            return builder.Build();
        }

        var lines = snapshot.Take(25).Select((entry, index) =>
        {
            var medal = index switch
            {
                0 => "🥇",
                1 => "🥈",
                2 => "🥉",
                _ => "✨"
            };
            var monthly = entry.MonthlyCents > 0 ? $" · {SponsorService.Money(entry.MonthlyCents)}/mo" : "";
            return $"{medal} **{SponsorService.DisplayName(entry)}** — {SponsorService.Money(entry.LifetimeCents)}"
                + $" total{monthly} · {entry.Platform}";
        });

        builder.WithDescription(string.Join('\n', lines));
        return builder.Build();
    }

    public async Task SyncRolesAsync(SocketGuild guild, SponsorState state)
    {
        var roles = await EnsureRolesAsync(guild);
        var topSponsor = state.Snapshot.FirstOrDefault();

        foreach (var entry in state.Snapshot)
        {
            if (!state.Links.TryGetValue(entry.Key, out var discordId)
                || guild.GetUser(discordId) is not { } member)
            {
                continue;
            }

            var earned = Tiers.FirstOrDefault(tier => entry.MonthlyCents >= tier.MinimumCents).Name;
            var wanted = new List<string>();
            if (earned != null)
            {
                wanted.Add(earned);
            }

            if (topSponsor != null && entry.Key == topSponsor.Key)
            {
                wanted.Add(TopSponsorRole);
            }

            foreach (var (name, role) in roles)
            {
                var hasRole = member.Roles.Any(memberRole => memberRole.Id == role.Id);
                if (wanted.Contains(name) && !hasRole)
                {
                    await member.AddRoleAsync(role);
                }
                else if (!wanted.Contains(name) && hasRole)
                {
                    await member.RemoveRoleAsync(role);
                }
            }
        }
    }

    private static async Task<Dictionary<string, IRole>> EnsureRolesAsync(SocketGuild guild)
    {
        var roles = new Dictionary<string, IRole>();
        foreach (var (name, _, color) in Tiers.Append((TopSponsorRole, 0, Color.Purple)))
        {
            var role = guild.Roles.FirstOrDefault(guildRole => guildRole.Name == name)
                ?? (IRole)await guild.CreateRoleAsync(name, color: color, isHoisted: true, isMentionable: false);
            roles[name] = role;
        }

        return roles;
    }
}
