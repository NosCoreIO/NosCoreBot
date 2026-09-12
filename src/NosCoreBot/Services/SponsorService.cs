using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace NosCoreBot.Services;

public record SponsorEntry(string Platform, string Id, string DisplayName, bool IsPublic, bool IsActive,
    int MonthlyCents, int LifetimeCents)
{
    public string Key => $"{Platform}:{Id}";
}

public class SponsorState
{
    public Dictionary<string, ulong> Links { get; set; } = new();

    public Dictionary<string, int> Lifetime { get; set; } = new();

    public List<SponsorEntry> Snapshot { get; set; } = new();

    public DateTime? LastReportUtc { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class SponsorService
{
    private const string StateKey = "sponsors.json";

    private const string SponsorsQuery = """
        query($login: String!, $cursor: String) {
          user(login: $login) {
            sponsorshipsAsMaintainer(first: 100, after: $cursor, includePrivate: true, activeOnly: false) {
              pageInfo { hasNextPage endCursor }
              nodes {
                isActive
                privacyLevel
                createdAt
                isOneTimePayment
                tier { monthlyPriceInCents }
                sponsorEntity {
                  __typename
                  ... on User { login name }
                  ... on Organization { login name }
                }
              }
            }
          }
        }
        """;

    private readonly HttpClient _httpClient;
    private readonly ILogger<SponsorService> _logger;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    public SponsorService(HttpClient httpClient, ILogger<SponsorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SponsorEntry>> FetchAsync()
    {
        var entries = new List<SponsorEntry>();
        entries.AddRange(await FetchGitHubAsync());
        entries.AddRange(await FetchPatreonAsync());
        return entries;
    }

    private async Task<List<SponsorEntry>> FetchGitHubAsync()
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_SPONSORS_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("GITHUB_SPONSORS_TOKEN is unset, skipping GitHub Sponsors");
            return new List<SponsorEntry>();
        }

        var login = Environment.GetEnvironmentVariable("GITHUB_SPONSORS_LOGIN") ?? "0Lucifer0";
        var entries = new List<SponsorEntry>();
        string cursor = null;

        do
        {
            var payload = JsonSerializer.Serialize(new
            {
                query = SponsorsQuery,
                variables = new { login, cursor }
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.UserAgent.ParseAdd("NosCoreBot");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub Sponsors returned {Status}", response.StatusCode);
                return entries;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                _logger.LogWarning("GitHub Sponsors query failed: {Errors}", errors.ToString());
                return entries;
            }

            var sponsorships = document.RootElement
                .GetProperty("data").GetProperty("user")
                .GetProperty("sponsorshipsAsMaintainer");

            foreach (var node in sponsorships.GetProperty("nodes").EnumerateArray())
            {
                var sponsor = node.GetProperty("sponsorEntity");
                if (sponsor.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var isActive = node.GetProperty("isActive").GetBoolean();
                var isOneTime = node.GetProperty("isOneTimePayment").GetBoolean();
                var createdAt = node.GetProperty("createdAt").GetDateTimeOffset();
                var monthlyCents = node.TryGetProperty("tier", out var tier) && tier.ValueKind == JsonValueKind.Object
                    ? tier.GetProperty("monthlyPriceInCents").GetInt32()
                    : 0;

                var id = sponsor.GetProperty("login").GetString()!;
                var name = sponsor.TryGetProperty("name", out var nameElement)
                    && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString()!
                        : id;

                entries.Add(new SponsorEntry("github", id, name,
                    node.GetProperty("privacyLevel").GetString() == "PUBLIC", isActive,
                    isActive && !isOneTime ? monthlyCents : 0,
                    isOneTime ? monthlyCents : monthlyCents * MonthsSince(createdAt)));
            }

            var pageInfo = sponsorships.GetProperty("pageInfo");
            cursor = pageInfo.GetProperty("hasNextPage").GetBoolean()
                ? pageInfo.GetProperty("endCursor").GetString()
                : null;
        } while (cursor != null);

        return entries;
    }

    private static int MonthsSince(DateTimeOffset createdAt)
    {
        return Math.Max(1, (int)Math.Floor((DateTimeOffset.UtcNow - createdAt).TotalDays / 30.44));
    }

    private async Task<List<SponsorEntry>> FetchPatreonAsync()
    {
        var token = Environment.GetEnvironmentVariable("PATREON_ACCESS_TOKEN");
        var campaignId = Environment.GetEnvironmentVariable("PATREON_CAMPAIGN_ID");
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(campaignId))
        {
            _logger.LogInformation("PATREON_ACCESS_TOKEN or PATREON_CAMPAIGN_ID is unset, skipping Patreon");
            return new List<SponsorEntry>();
        }

        var namesArePublic = Environment.GetEnvironmentVariable("PATREON_NAMES_PUBLIC") == "true";
        var entries = new List<SponsorEntry>();
        var url = $"https://www.patreon.com/api/oauth2/v2/campaigns/{campaignId}/members"
            + "?fields%5Bmember%5D=full_name,patron_status,currently_entitled_amount_cents,lifetime_support_cents"
            + "&page%5Bcount%5D=100";

        while (url != null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Patreon returned {Status}", response.StatusCode);
                return entries;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            foreach (var member in document.RootElement.GetProperty("data").EnumerateArray())
            {
                var attributes = member.GetProperty("attributes");
                var lifetime = attributes.GetProperty("lifetime_support_cents").GetInt32();
                if (lifetime <= 0)
                {
                    continue;
                }

                var isActive = attributes.GetProperty("patron_status").GetString() == "active_patron";
                entries.Add(new SponsorEntry("patreon", member.GetProperty("id").GetString()!,
                    attributes.GetProperty("full_name").GetString() ?? "Patron", namesArePublic, isActive,
                    isActive ? attributes.GetProperty("currently_entitled_amount_cents").GetInt32() : 0, lifetime));
            }

            url = NextPatreonPage(document);
        }

        return entries;
    }

    private static string NextPatreonPage(JsonDocument document)
    {
        return document.RootElement.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("pagination", out var pagination)
            && pagination.TryGetProperty("cursors", out var cursors)
            && cursors.TryGetProperty("next", out var next)
            && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;
    }

    public static List<SponsorEntry> ApplyLifetimeFloor(SponsorState state, IEnumerable<SponsorEntry> fetched)
    {
        var entries = new List<SponsorEntry>();
        foreach (var entry in fetched)
        {
            var known = state.Lifetime.TryGetValue(entry.Key, out var stored) ? stored : 0;
            var lifetime = entry.IsActive
                ? Math.Max(known, entry.LifetimeCents)
                : known > 0
                    ? known
                    : entry.LifetimeCents;
            state.Lifetime[entry.Key] = lifetime;
            entries.Add(entry with { LifetimeCents = lifetime });
        }

        return entries
            .OrderByDescending(entry => entry.LifetimeCents)
            .ThenByDescending(entry => entry.MonthlyCents)
            .ToList();
    }

    public async Task<SponsorState> LoadStateAsync()
    {
        try
        {
            using var client = CreateS3Client();
            using var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                Key = StateKey
            });
            await using var stream = response.ResponseStream;
            return await JsonSerializer.DeserializeAsync<SponsorState>(stream) ?? new SponsorState();
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new SponsorState();
        }
    }

    public async Task<SponsorState> MutateAsync(Func<SponsorState, Task> mutation)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();
            await mutation(state);
            await SaveStateAsync(state);
            return state;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public Task<SponsorState> MutateAsync(Action<SponsorState> mutation)
    {
        return MutateAsync(state =>
        {
            mutation(state);
            return Task.CompletedTask;
        });
    }

    private async Task SaveStateAsync(SponsorState state)
    {
        state.UpdatedAt = DateTime.UtcNow;
        using var client = CreateS3Client();
        await using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(state));
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
            Key = StateKey,
            ContentType = "application/json",
            InputStream = stream
        });
    }

    private static AmazonS3Client CreateS3Client()
    {
        return new AmazonS3Client(new BasicAWSCredentials(
            Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
            Environment.GetEnvironmentVariable("S3_SECRET_KEY")), RegionEndpoint.USWest2);
    }

    public static string DisplayName(SponsorEntry entry)
    {
        return entry.IsPublic ? entry.DisplayName : "Anonymous";
    }

    public static string Money(int cents)
    {
        return (cents / 100m).ToString("C0", CultureInfo.GetCultureInfo("en-US"));
    }
}
