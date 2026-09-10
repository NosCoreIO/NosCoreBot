using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NosCoreBot.Services;

namespace NosCoreBot.Modules;

public class SponsorModule : ModuleBase<SocketCommandContext>
{
    private readonly SponsorService _sponsors;
    private readonly SponsorSyncService _sync;

    public SponsorModule(SponsorService sponsors, SponsorSyncService sync)
    {
        _sponsors = sponsors;
        _sync = sync;
    }

    [Command("sponsors")]
    [Name("sponsors")]
    [Summary("show the sponsor leaderboard")]
    public async Task ShowSponsors()
    {
        var state = await _sponsors.LoadStateAsync();
        await ReplyAsync("", false, SponsorSyncService.BuildEmbed(state.Snapshot));
    }

    [Command("link-sponsor")]
    [Name("link-sponsor <user> <platform> <id>")]
    [Summary("link a discord member to a sponsor account so they get their role")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task LinkSponsor(SocketGuildUser user, string platform, string id)
    {
        var state = await _sponsors.LoadStateAsync();
        var key = $"{platform.ToLowerInvariant()}:{id}";
        state.Links[key] = user.Id;
        await _sponsors.SaveStateAsync(state);
        await ReplyAsync($"Linked {user.Mention} to `{key}`. Roles apply on the next sync.");
    }

    [Command("unlink-sponsor")]
    [Name("unlink-sponsor <platform> <id>")]
    [Summary("remove a sponsor account link")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task UnlinkSponsor(string platform, string id)
    {
        var state = await _sponsors.LoadStateAsync();
        var key = $"{platform.ToLowerInvariant()}:{id}";
        if (!state.Links.Remove(key))
        {
            await ReplyAsync($"No link for `{key}`.");
            return;
        }

        await _sponsors.SaveStateAsync(state);
        await ReplyAsync($"Unlinked `{key}`.");
    }

    [Command("sponsor-sync")]
    [Name("sponsor-sync")]
    [Summary("refresh sponsors, post the leaderboard and apply roles now")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task SyncNow()
    {
        var state = await _sponsors.LoadStateAsync();
        await _sync.RunAsync(state);
        await ReplyAsync($"Synced {state.Snapshot.Count} sponsors.");
    }

    [Command("sponsor-links")]
    [Name("sponsor-links")]
    [Summary("list the sponsor account links")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task ShowLinks()
    {
        var state = await _sponsors.LoadStateAsync();
        if (state.Links.Count == 0)
        {
            await ReplyAsync("No sponsor links yet.");
            return;
        }

        var lines = state.Links.Select(link => $"`{link.Key}` -> <@{link.Value}>");
        await ReplyAsync(string.Join('\n', lines));
    }
}
