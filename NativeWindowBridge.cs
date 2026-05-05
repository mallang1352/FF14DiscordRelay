using System.Runtime.InteropServices;

namespace FF14DiscordRelay;

internal static class NativeWindowBridge
{
    private const string ShowSettingsMessageName = "FF14DiscordRelay.ShowSettings.20260502";
    private static readonly Lazy<int> ShowSettingsMessageId = new(() => RegisterWindowMessage(ShowSettingsMessageName));

    public static int ShowSettingsMessage => ShowSettingsMessageId.Value;

    public static void NotifyExistingInstance()
    {
        _ = PostMessage(new IntPtr(0xffff), ShowSettingsMessage, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
