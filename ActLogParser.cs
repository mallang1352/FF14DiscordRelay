using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FF14DiscordRelay;

public static partial class ActLogParser
{
    public static ChatMessage? TryParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var network = TryParseNetworkLine(line);
        if (network is not null)
            return network;

        return TryParseParsedLine(line);
    }

    public static IReadOnlyList<string> DetectNicknameCandidates(string folder, IReadOnlyCollection<string> enabledCodes)
    {
        if (!Directory.Exists(folder))
            return [];

        var codes = new HashSet<string>(enabledCodes.Select(ChannelCatalog.NormalizeCode), StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(folder, "*.log").OrderByDescending(File.GetLastWriteTime).Take(8))
        {
            foreach (var line in ReadRecentLines(file, 3000).Reverse())
            {
                var primaryPlayer = TryParsePrimaryPlayerName(line);
                if (!string.IsNullOrWhiteSpace(primaryPlayer))
                {
                    AddCandidate(primaryPlayer);
                    continue;
                }

                var msg = TryParse(line);
                if (msg is null || string.IsNullOrWhiteSpace(msg.Sender))
                    continue;
                if (codes.Count > 0 && !codes.Contains(msg.CodeHex))
                    continue;

                AddCandidate(msg.Sender);
            }
        }

        AddConfigNameCandidates(AddCandidate);
        return candidates.Take(10).ToArray();

        void AddCandidate(string value)
        {
            var clean = Clean(value);
            if (!IsLikelyPlayerName(clean))
                return;
            if (seen.Add(clean))
                candidates.Add(clean);
        }
    }

    public static string? AutoFindActLogFolder()
    {
        return ProcessHelper.DetectActLogFolder();
    }

    public static string? DetectLatestPrimaryPlayerName(string folder)
    {
        if (!Directory.Exists(folder))
            return null;

        foreach (var file in Directory.GetFiles(folder, "*.log").OrderByDescending(File.GetLastWriteTime).Take(4))
        {
            foreach (var line in ReadRecentLines(file, 3000).Reverse())
            {
                var primaryPlayer = TryParsePrimaryPlayerName(line);
                if (!string.IsNullOrWhiteSpace(primaryPlayer))
                    return primaryPlayer;
            }
        }

        return null;
    }

    public static string? TryParsePrimaryPlayerName(string line)
    {
        if (!line.StartsWith("02|", StringComparison.Ordinal))
            return null;

        var parts = line.Split('|');
        if (parts.Length < 4)
            return null;

        return IsLikelyPlayerName(parts[3]) ? Clean(parts[3]) : null;
    }

    private static ChatMessage? TryParseNetworkLine(string line)
    {
        if (!line.StartsWith("00|", StringComparison.Ordinal))
            return null;

        var parts = line.Split('|');
        if (parts.Length < 5)
            return null;

        var code = ChannelCatalog.NormalizeCode(parts[2]);
        return new ChatMessage(ParseTimestamp(parts[1]), code, ChannelCatalog.GetDisplayName(code), Clean(parts[3]), Clean(parts[4]));
    }

    private static ChatMessage? TryParseParsedLine(string line)
    {
        var match = ParsedChatLogRegex().Match(line);
        if (!match.Success)
            return null;

        var code = ChannelCatalog.NormalizeCode(match.Groups["code"].Value);
        return new ChatMessage(null, code, ChannelCatalog.GetDisplayName(code), Clean(match.Groups["name"].Value), Clean(match.Groups["text"].Value));
    }

    private static DateTimeOffset? ParseTimestamp(string value)
    {
        return DateTimeOffset.TryParse(value, out var timestamp) ? timestamp : null;
    }

    private static string Clean(string value)
    {
        return value.Replace('\u0000', ' ').Trim();
    }

    private static bool IsLikelyPlayerName(string value)
    {
        var name = Clean(value);
        if (name.Length < 2)
            return false;
        if (name.Equals("YOU", StringComparison.OrdinalIgnoreCase))
            return false;
        if (name.Contains("dummy", StringComparison.OrdinalIgnoreCase))
            return false;

        return name.Any(char.IsLetter);
    }

    private static void AddConfigNameCandidates(Action<string> addCandidate)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Advanced Combat Tracker", "Config", "Advanced Combat Tracker.config.xml");
        if (!File.Exists(path))
            return;

        try
        {
            var doc = XDocument.Load(path);
            foreach (var element in doc.Descendants())
            {
                var name = element.Attribute("Name")?.Value ?? "";
                var value = element.Attribute("Value")?.Value ?? "";
                if (!name.Equals("tbCharName", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!IsLikelyPlayerName(value))
                    continue;

                addCandidate(value);
            }
        }
        catch
        {
            // ACT config is optional for nickname detection.
        }
    }

    private static IEnumerable<string> ReadRecentLines(string path, int maxLines)
    {
        try
        {
            var queue = new Queue<string>(maxLines);
            foreach (var line in File.ReadLines(path, Encoding.UTF8))
            {
                if (queue.Count == maxLines)
                    queue.Dequeue();
                queue.Enqueue(line);
            }
            return queue;
        }
        catch
        {
            return [];
        }
    }

    [GeneratedRegex(@"ChatLog\s+00:(?<code>[^:]*):(?<name>[^:]*):(?<text>[^:]*)", RegexOptions.Compiled)]
    private static partial Regex ParsedChatLogRegex();
}
