using static FF14DiscordRelay.UiText;

namespace FF14DiscordRelay;

public sealed class CreatePasswordForm : Form
{
    private readonly TextBox _passwordBox = DialogChrome.TextInput();
    private readonly TextBox _confirmBox = DialogChrome.TextInput();

    public string Password => _passwordBox.Text;

    public CreatePasswordForm(string title = "")
    {
        Text = string.IsNullOrWhiteSpace(title) ? K("67mE67CA67KI7Zi4IOyDneyEsQ==") : title;
        DialogChrome.Apply(this, 500, 340);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 8,
            BackColor = UiPalette.PageBack,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(DialogChrome.Header(AppIdentity.DisplayName, K("7LKY7J2MIOyggOyepe2VoCDruYTrsIDrsojtmLjrpbwg7ISk7KCV7ZWY7IS47JqULg==")), 0, 0);
        layout.Controls.Add(DialogChrome.FieldLabel(K("67mE67CA67KI7Zi4")), 0, 1);
        _passwordBox.Margin = new Padding(0, 5, 0, 12);
        layout.Controls.Add(_passwordBox, 0, 2);
        layout.Controls.Add(DialogChrome.FieldLabel(K("67mE67CA67KI7Zi4IO2ZleyduA==")), 0, 3);
        _confirmBox.Margin = new Padding(0, 5, 0, 0);
        layout.Controls.Add(_confirmBox, 0, 4);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = UiPalette.PageBack,
            Margin = new Padding(0, 4, 0, 0),
        };
        var cancel = DialogChrome.SecondaryButton(K("7Leo7IaM"));
        cancel.DialogResult = DialogResult.Cancel;
        var ok = DialogChrome.PrimaryButton(K("7KCA7J6l"));
        ok.DialogResult = DialogResult.OK;
        ok.Click += (_, _) =>
        {
            if (_passwordBox.Text.Length < 4)
            {
                MessageBox.Show(K("67mE67CA67KI7Zi464qUIDTsnpAg7J207IOB7Jy866GcIOyeheugpe2VmOyEuOyalC4="), K("67mE67CA67KI7Zi4"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }

            if (_passwordBox.Text != _confirmBox.Text)
            {
                MessageBox.Show(K("67mE67CA67KI7Zi4IO2ZleyduOydtCDsnbzsuZjtlZjsp4Ag7JWK7Iq164uI64ukLg=="), K("67mE67CA67KI7Zi4"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };

        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons, 0, 6);

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(layout);
    }
}
