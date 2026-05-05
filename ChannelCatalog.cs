namespace FF14DiscordRelay;

public sealed record ChannelDefinition(string Key, string DisplayName, string CodeHex, string Group);

public static class ChannelCatalog
{
    public static readonly IReadOnlyList<ChannelDefinition> All =
    [
        new("say", UiText.K("66eQ7ZWY6riw"), "000A", "Basic"),
        new("shout", UiText.K("7Jm47LmY6riw"), "000B", "Basic"),
        new("tell_out", UiText.K("6reT7IaN66eQIOuztOuCtOq4sA=="), "000C", "Basic"),
        new("tell_in", UiText.K("6reT7IaN66eQIOuwm+q4sA=="), "000D", "Basic"),
        new("party", UiText.K("7YyM7Yuw"), "000E", "Basic"),
        new("alliance", UiText.K("7Jew7ZWpIO2MjO2LsA=="), "000F", "Basic"),
        new("fc", UiText.K("7J6Q7Jyg67aA64yA"), "0018", "Basic"),
        new("novice", UiText.K("7LSI67O07J6QIOyxhOuEkA=="), "001B", "Basic"),
        new("yell", UiText.K("65ag65Ok6riw"), "001E", "Basic"),
        new("cross_party", UiText.K("7ISc67KE7LSI7JuUIO2MjO2LsA=="), "0020", "Basic"),
        new("pvp", UiText.K("UHZQIO2MgA=="), "0024", "Basic"),
        new("echo", UiText.K("7JeQ7L2U"), "0038", "Basic"),

        new("ls1", UiText.K("66eB7YGs7ImYIDE="), "0010", "LS"),
        new("ls2", UiText.K("66eB7YGs7ImYIDI="), "0011", "LS"),
        new("ls3", UiText.K("66eB7YGs7ImYIDM="), "0012", "LS"),
        new("ls4", UiText.K("66eB7YGs7ImYIDQ="), "0013", "LS"),
        new("ls5", UiText.K("66eB7YGs7ImYIDU="), "0014", "LS"),
        new("ls6", UiText.K("66eB7YGs7ImYIDY="), "0015", "LS"),
        new("ls7", UiText.K("66eB7YGs7ImYIDc="), "0016", "LS"),
        new("ls8", UiText.K("66eB7YGs7ImYIDg="), "0017", "LS"),

        new("cwls1", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCAx"), "0025", "CWLS"),
        new("cwls2", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCAy"), "0065", "CWLS"),
        new("cwls3", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCAz"), "0066", "CWLS"),
        new("cwls4", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCA0"), "0067", "CWLS"),
        new("cwls5", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCA1"), "0068", "CWLS"),
        new("cwls6", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCA2"), "0069", "CWLS"),
        new("cwls7", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCA3"), "006A", "CWLS"),
        new("cwls8", UiText.K("7ISc67KE7LSI7JuUIOunge2BrOyJmCA4"), "006B", "CWLS"),
    ];

    public static readonly IReadOnlyList<ChannelDefinition> Linkshell = All.Where(x => x.Group == "LS").ToArray();
    public static readonly IReadOnlyList<ChannelDefinition> CrossWorldLinkshell = All.Where(x => x.Group == "CWLS").ToArray();

    public static string GetDisplayName(string codeHex)
    {
        var normalized = NormalizeCode(codeHex);
        return All.FirstOrDefault(x => x.CodeHex.Equals(normalized, StringComparison.OrdinalIgnoreCase))?.DisplayName
            ?? normalized;
    }

    public static string NormalizeCode(string code)
    {
        var trimmed = code.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];

        return trimmed.ToUpperInvariant().PadLeft(4, '0');
    }
}
