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
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace NosCoreBot.Services;

public record SponsorEntry(string Platform, string Id, string DisplayName, bool IsPublic, int MonthlyCents,
    int LifetimeCents)
{
    public string Key => $"{Platform}:{Id}";
}

public class SponsorState
{
    public Dictionary<string, ulong> Links { get; set; } = new();

    public List<SponsorEntry> Snapshot { get; set; } = new();

    public DateTime? LastReportUtc { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class SponsorService
{
    private const string StateKey = "sponsors.json";

    private const string SponsorsQuery = """
        query($login: String!) {
          user(login: $login) {
            sponsorshipsAsMaintainer(first: 100, includePrivate: true, activeOnly: false) {
              nodes {
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
        return entries
            .OrderByDescending(entry => entry.LifetimeCents)
            .ThenByDescending(entry => entry.MonthlyCents)
            .ToList();
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
        var payload = JsonSerializer.Serialize(new
        {
            query = SponsorsQuery,
            variables = new { login }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("NosCoreBot");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GitHub Sponsors returned {Status}", response.StatusCode);
            return new List<SponsorEntry>();
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (document.RootElement.TryGetProperty("errors", out var errors))
        {
            _logger.LogWarning("GitHub Sponsors query failed: {Errors}", errors.ToString());
            return new List<SponsorEntry>();
        }

        var entries = new List<SponsorEntry>();
        var nodes = document.RootElement
            .GetProperty("data").GetProperty("user")
            .GetProperty("sponsorshipsAsMaintainer").GetProperty("nodes");

        foreach (var node in nodes.EnumerateArray())
        {
            var isPublic = node.GetProperty("privacyLevel").GetString() == "PUBLIC";
            var isOneTime = node.GetProperty("isOneTimePayment").GetBoolean();
            var createdAt = node.GetProperty("createdAt").GetDateTimeOffset();
            var monthlyCents = node.TryGetProperty("tier", out var tier) && tier.ValueKind == JsonValueKind.Object
                ? tier.GetProperty("monthlyPriceInCents").GetInt32()
                : 0;

            var sponsor = node.GetProperty("sponsorEntity");
            if (sponsor.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = sponsor.GetProperty("login").GetString()!;
            var name = sponsor.TryGetProperty("name", out var nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                    ? nameElement.GetString()!
                    : id;

            entries.Add(new SponsorEntry("github", id, name, isPublic,
                isOneTime ? 0 : monthlyCents,
                isOneTime ? monthlyCents : monthlyCents * MonthsSince(createdAt)));
        }

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
        var url = $"https://www.patreon.com/api/oauth2/v2/campaigns/{campaignId}/members"
            + "?fields%5Bmember%5D=full_name,patron_status,currently_entitled_amount_cents,lifetime_support_cents"
            + "&page%5Bcount%5D=100";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Patreon returned {Status}", response.StatusCode);
            return new List<SponsorEntry>();
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entries = new List<SponsorEntry>();
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
                attributes.GetProperty("full_name").GetString() ?? "Patron", namesArePublic,
                isActive ? attributes.GetProperty("currently_entitled_amount_cents").GetInt32() : 0, lifetime));
        }

        return entries;
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

    public async Task SaveStateAsync(SponsorState state)
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
