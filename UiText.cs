using System.Text;

namespace FF14DiscordRelay;

public static class UiText
{
    public static string K(string base64)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}

