using System.Text.Json.Serialization;

namespace FF14DiscordRelay;

public sealed class AppConfig
{
    public string WebhookUrl { get; set; } = "";
    public string VerifiedWebhookHash { get; set; } = "";
    public string MessageTemplate { get; set; } = "{message}";
    public bool DisableMentions { get; set; } = true;
    public bool SplitLongMessages { get; set; } = true;

    public string ActLogFolder { get; set; } = "";
    public List<string> ActProcessNames { get; set; } = ["Advanced Combat Tracker", "ACTx86", "ACTx64", "Advanced Combat Tracker.exe"];
    public bool AutoDetectNickname { get; set; } = true;
    public string Nickname { get; set; } = "";

    public bool OwnMessagesOnly { get; set; } = true;
    public List<string> EnabledChannelCodes { get; set; } = [];

    public bool StartMinimizedToTray { get; set; } = true;
    public bool SaveLocalRelayLog { get; set; } = false;
    public int DuplicateWindowSeconds { get; set; } = 2;

    [JsonIgnore]
    public bool HasRequiredStartSettings =>
        !string.IsNullOrWhiteSpace(WebhookUrl)
        && !string.IsNullOrWhiteSpace(ActLogFolder)
        && EnabledChannelCodes.Count > 0
        && (!OwnMessagesOnly || !string.IsNullOrWhiteSpace(Nickname));

    public static AppConfig CreateDefault()
    {
        var config = new AppConfig();
        config.EnabledChannelCodes.Add(ChannelCatalog.CrossWorldLinkshell[0].CodeHex);
        return config;
    }
}

public sealed record ChatMessage(DateTimeOffset? Timestamp, string CodeHex, string ChannelName, string Sender, string Text);
