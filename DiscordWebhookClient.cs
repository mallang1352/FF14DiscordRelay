using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FF14DiscordRelay;

public sealed class DiscordWebhookClient
{
    private static readonly HttpClient Client = new();

    public static bool IsMissingWebhook(Exception ex)
    {
        return ex is DiscordWebhookException { IsMissingWebhook: true }
            || ex.InnerException is not null && IsMissingWebhook(ex.InnerException);
    }

    public async Task SendAsync(AppConfig config, string content, CancellationToken cancellationToken)
    {
        foreach (var chunk in SplitContent(content, config.SplitLongMessages ? 1900 : 2000))
            await SendChunkAsync(config, chunk, cancellationToken);
    }

    private async Task SendChunkAsync(AppConfig config, string content, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["content"] = content,
        };

        if (config.DisableMentions)
            payload["allowed_mentions"] = new { parse = Array.Empty<string>() };

        using var response = await Client.PostAsJsonAsync(config.WebhookUrl, payload, cancellationToken);
        if (response.StatusCode == (HttpStatusCode)429)
        {
            var retrySeconds = await ReadRetryAfterAsync(response, cancellationToken);
            throw new DiscordRateLimitException(retrySeconds);
        }

        await EnsureWebhookSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureWebhookSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = "";
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
        }

        throw new DiscordWebhookException(response.StatusCode, body);
    }

    private static async Task<double> ReadRetryAfterAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return Math.Max(0.5, delta.TotalSeconds);

        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (json.TryGetProperty("retry_after", out var retryAfter))
                return retryAfter.GetDouble();
        }
        catch
        {
            // Fall through to a conservative default.
        }

        return 2.0;
    }

    private static IEnumerable<string> SplitContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
        {
            yield return content;
            yield break;
        }

        for (var i = 0; i < content.Length; i += maxLength)
            yield return content.Substring(i, Math.Min(maxLength, content.Length - i));
    }
}

public sealed class DiscordRateLimitException : HttpRequestException
{
    public double RetryAfterSeconds { get; }

    public DiscordRateLimitException(double retryAfterSeconds)
        : base($"Discord Webhook rate limited. Retry after {retryAfterSeconds:0.###} seconds.")
    {
        RetryAfterSeconds = Math.Max(0.5, retryAfterSeconds);
    }
}

public sealed class DiscordWebhookException : HttpRequestException
{
    public HttpStatusCode StatusCodeValue { get; }
    public string ResponseBody { get; }

    public bool IsMissingWebhook => StatusCodeValue is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    public DiscordWebhookException(HttpStatusCode statusCode, string responseBody)
        : base($"Discord Webhook request failed: {(int)statusCode} {statusCode}")
    {
        StatusCodeValue = statusCode;
        ResponseBody = responseBody;
    }
}
