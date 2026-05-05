namespace FF14DiscordRelay;

internal static class UiImages
{
    private static readonly Lazy<Icon> AppIcon = new(LoadAppIcon);
    private static readonly Lazy<Image> AppDisplayImage = new(LoadAppDisplayImage);

    public static Icon Icon => AppIcon.Value;
    public static Image DisplayImage => AppDisplayImage.Value;

    private static Icon LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app_icon.ico");
        if (File.Exists(iconPath))
            return new Icon(iconPath);

        var resourceName = typeof(UiImages).Assembly
            .GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith("Assets.app_icon.ico", StringComparison.OrdinalIgnoreCase));
        if (resourceName is not null)
        {
            var stream = typeof(UiImages).Assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
                return new Icon(stream);
        }

        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private static Image LoadAppDisplayImage()
    {
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app_icon.png");
        if (File.Exists(pngPath))
            return LoadBitmapFromBytes(File.ReadAllBytes(pngPath));

        var resourceName = typeof(UiImages).Assembly
            .GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith("Assets.app_icon.png", StringComparison.OrdinalIgnoreCase));
        if (resourceName is not null)
        {
            using var stream = typeof(UiImages).Assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return LoadBitmapFromBytes(memory.ToArray());
            }
        }

        return AppIcon.Value.ToBitmap();
    }

    private static Bitmap LoadBitmapFromBytes(byte[] bytes)
    {
        using var memory = new MemoryStream(bytes);
        using var source = Image.FromStream(memory, useEmbeddedColorManagement: true, validateImageData: true);
        return new Bitmap(source);
    }
}
