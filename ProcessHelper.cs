using System.Diagnostics;
using System.Security.Principal;
using System.Xml.Linq;
using static FF14DiscordRelay.UiText;

namespace FF14DiscordRelay;

public sealed record ActDetectionResult(bool IsRunning, string ProcessName, string? ProcessPath, string? LogFolder, string Message);

public static class ProcessHelper
{
    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool IsActRunning(IEnumerable<string> processNames)
    {
        var normalized = processNames
            .Select(Normalize)
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalized.Count == 0)
            return false;

        return Process.GetProcesses().Any(process =>
        {
            try
            {
                return normalized.Contains(Normalize(process.ProcessName));
            }
            catch
            {
                return false;
            }
        });
    }

    public static ActDetectionResult DetectAct()
    {
        var process = Process.GetProcesses()
            .Where(IsLikelyActProcess)
            .OrderBy(x => x.ProcessName.Equals("Advanced Combat Tracker", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .FirstOrDefault();

        var processName = process?.ProcessName ?? "";
        var processPath = TryGetProcessPath(process);
        var logFolder = DetectActLogFolder();

        if (process is null)
            return new ActDetectionResult(false, "", processPath, logFolder, logFolder is null ? K("QUNU66W8IOywvuyngCDrqrvtlojsirXri4jri6Qu") : K("66Gc6re4IO2PtOuNlOuKlCDssL7slZjsp4Drp4wgQUNU6rCAIOyLpO2WiSDspJHsnbQg7JWE64uZ64uI64ukLg=="));

        return new ActDetectionResult(true, processName, processPath, logFolder, logFolder is null ? K("QUNU6rCAIOyLpO2WiSDspJHsnbTsp4Drp4wg66Gc6re4IO2PtOuNlOulvCDssL7sp4Ag66q77ZaI7Iq164uI64ukLg==") : K("QUNU7JmAIOuhnOq3uCDtj7TrjZTrpbwg6rCQ7KeA7ZaI7Iq164uI64ukLg=="));
    }

    public static string? DetectActLogFolder()
    {
        var folders = new List<string>();
        var roaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Advanced Combat Tracker", "Config");

        AddFolderFromActConfig(Path.Combine(roaming, "FFXIV_ACT_Plugin.config.xml"), folders);
        AddFolderFromActConfig(Path.Combine(roaming, "Advanced Combat Tracker.config.xml"), folders);

        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Advanced Combat Tracker", "FFXIVLogs"));
        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Advanced Combat Tracker", "FFXIVLogs"));

        return folders
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Environment.ExpandEnvironmentVariables)
            .Select(x => x.Trim())
            .Select(x => x.TrimEnd('\\'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(Directory.Exists);
    }

    private static bool IsLikelyActProcess(Process process)
    {
        try
        {
            var name = process.ProcessName;
            return name.Equals("Advanced Combat Tracker", StringComparison.OrdinalIgnoreCase)
                || name.Equals("ACTx86", StringComparison.OrdinalIgnoreCase)
                || name.Equals("ACTx64", StringComparison.OrdinalIgnoreCase)
                || (name.Contains("Combat", StringComparison.OrdinalIgnoreCase) && name.Contains("Tracker", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private static string? TryGetProcessPath(Process? process)
    {
        if (process is null)
            return null;

        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static void AddFolderFromActConfig(string path, List<string> folders)
    {
        if (!File.Exists(path))
            return;

        try
        {
            var doc = XDocument.Load(path);
            foreach (var element in doc.Descendants())
            {
                var name = element.Attribute("Name")?.Value ?? "";
                var value = element.Attribute("Value")?.Value ?? "";
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (name.Equals("txtLogFileDirectory", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("folderLogs", StringComparison.OrdinalIgnoreCase))
                {
                    folders.Add(value);
                }
                else if (name.Equals("logFilePath", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = Path.GetDirectoryName(value);
                    if (!string.IsNullOrWhiteSpace(dir))
                        folders.Add(dir);
                }
            }
        }
        catch
        {
            // Broken ACT config should not prevent manual setup.
        }
    }

    private static string Normalize(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^4];
        return trimmed;
    }
}
