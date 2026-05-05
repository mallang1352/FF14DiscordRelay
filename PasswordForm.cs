using static FF14DiscordRelay.UiText;

namespace FF14DiscordRelay;

public sealed class PasswordForm : Form
{
    private readonly TextBox _passwordBox = DialogChrome.TextInput();

    public string Password => _passwordBox.Text;

    public PasswordForm()
    {
        Text = AppIdentity.DisplayName + " - " + K("67mE67CA67KI7Zi4");
        DialogChrome.Apply(this, 500, 250);
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 6,
            BackColor = UiPalette.PageBack,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(DialogChrome.Header(AppIdentity.DisplayName, K("7KCA7J6l65CcIOyEpOygleydtCDsnojsirXri4jri6QuIOu5hOuwgOuyiO2YuOulvCDsnoXroKXtlZjshLjsmpQ=")), 0, 0);
        layout.Controls.Add(DialogChrome.FieldLabel(K("67mE67CA67KI7Zi4")), 0, 1);
        _passwordBox.Margin = new Padding(0, 5, 0, 0);
        layout.Controls.Add(_passwordBox, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = UiPalette.PageBack,
            Margin = new Padding(0, 0, 0, 0),
        };
        var exit = DialogChrome.SecondaryButton(K("7KKF66OM"));
        exit.DialogResult = DialogResult.Cancel;
        var reset = DialogChrome.SecondaryButton(K("7ISk7KCVIOy0iOq4sO2ZlA=="), 116);
        var ok = DialogChrome.PrimaryButton(K("7ZmV7J24"));
        ok.DialogResult = DialogResult.OK;
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_passwordBox.Text))
            {
                MessageBox.Show(K("67mE67CA67KI7Zi466W8IOyeheugpe2VmOyEuOyalC4="), K("67mE67CA67KI7Zi4"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
            }
        };
        reset.Click += (_, _) => DialogResult = DialogResult.Retry;
        buttons.Controls.Add(exit);
        buttons.Controls.Add(reset);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons, 0, 4);

        AcceptButton = ok;
        CancelButton = exit;
        Controls.Add(layout);
    }
}
