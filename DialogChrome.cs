namespace FF14DiscordRelay;

internal static class DialogChrome
{
    public static void Apply(Form form, int width, int height)
    {
        form.Width = width;
        form.Height = height;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterParent;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.Font = new Font("Segoe UI", 9F);
        form.BackColor = UiPalette.PageBack;
        form.ForeColor = UiPalette.TextMain;

        try
        {
            form.Icon = UiImages.Icon;
        }
        catch
        {
            form.Icon = SystemIcons.Application;
        }
    }

    public static Control Header(string title, string subtitle)
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 0, 0, 14),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var icon = new IconPreview
        {
            Image = UiImages.DisplayImage,
            Width = 44,
            Height = 44,
            Margin = new Padding(0, 0, 12, 0),
        };

        var copy = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, RowCount = 2 };
        copy.BackColor = UiPalette.PageBack;
        copy.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = UiPalette.TextMain,
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            BackColor = UiPalette.PageBack,
            Margin = new Padding(0, 1, 0, 2),
        }, 0, 0);
        copy.Controls.Add(new Label
        {
            Text = subtitle,
            AutoSize = true,
            ForeColor = UiPalette.TextMuted,
            Font = new Font("Segoe UI", 9F),
            BackColor = UiPalette.PageBack,
            Margin = new Padding(1, 0, 0, 0),
        }, 0, 1);

        header.Controls.Add(icon, 0, 0);
        header.Controls.Add(copy, 1, 0);
        return header;
    }

    public static Label FieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = UiPalette.TextMuted,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(0, 8, 0, 3),
            BackColor = UiPalette.PageBack,
        };
    }

    public static TextBox TextInput()
    {
        return new TextBox
        {
            UseSystemPasswordChar = true,
            Width = 340,
            Height = 28,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = UiPalette.FieldBack,
            ForeColor = UiPalette.TextMain,
        };
    }

    public static Button PrimaryButton(string text)
    {
        var button = BaseButton(text);
        button.BackColor = UiPalette.Accent;
        button.ForeColor = Color.FromArgb(32, 25, 15);
        if (button is RoundedButton rounded)
            rounded.BorderColor = Color.FromArgb(217, 141, 26);
        return button;
    }

    public static Button SecondaryButton(string text, int width = 90)
    {
        var button = BaseButton(text, width);
        button.BackColor = Color.FromArgb(255, 248, 234);
        button.ForeColor = UiPalette.AccentDark;
        if (button is RoundedButton rounded)
            rounded.BorderColor = Color.FromArgb(232, 198, 132);
        return button;
    }

    private static Button BaseButton(string text, int width = 90)
    {
        return new RoundedButton
        {
            Text = text,
            Width = width,
            Height = 36,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
        };
    }

}
