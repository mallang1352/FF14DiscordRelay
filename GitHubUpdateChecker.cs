using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace FF14DiscordRelay;

internal static class GitHubUpdateChecker
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/mallang1352/FF14DiscordRelay/releases/latest";

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    public static async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FF14DiscordRelay", GetCurrentVersionText()));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await Client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, JsonOptions.Default, cancellationToken);
        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            return null;

        var latestVersion = ParseVersion(release.TagName);
        var currentVersion = GetCurrentVersion();
        if (latestVersion is null || currentVersion is null || latestVersion <= currentVersion)
            return null;

        return new UpdateInfo(
            release.TagName,
            currentVersion.ToString(3),
            release.HtmlUrl,
            release.Assets.FirstOrDefault(x => x.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))?.BrowserDownloadUrl);
    }

    private static Version? GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version;
    }

    private static string GetCurrentVersionText()
    {
        return GetCurrentVersion()?.ToString(3) ?? "0.0.0";
    }

    private static Version? ParseVersion(string tag)
    {
        var clean = tag.Trim();
        if (clean.StartsWith('v') || clean.StartsWith('V'))
            clean = clean[1..];

        return Version.TryParse(clean, out var version) ? version : null;
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }

    private sealed class GitHubRelease
    {
        public string TagName { get; set; } = "";
        public string HtmlUrl { get; set; } = "";
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubAsset
    {
        public string Name { get; set; } = "";
        public string BrowserDownloadUrl { get; set; } = "";
    }
}

internal sealed record UpdateInfo(string LatestVersion, string CurrentVersion, string ReleasePageUrl, string? DownloadUrl);
