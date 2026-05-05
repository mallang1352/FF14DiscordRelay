using static FF14DiscordRelay.UiText;

namespace FF14DiscordRelay;

static class Program
{
    private const string SingleInstanceMutexName = @"Global\FF14DiscordRelay.Portable.SingleInstance";

    [System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            if (!SingleInstanceSignal.NotifyExistingInstance())
                NativeWindowBridge.NotifyExistingInstance();
            return;
        }

        using var singleInstanceSignal = new SingleInstanceSignal();
        _ = SetCurrentProcessExplicitAppUserModelID(AppIdentity.AppUserModelId);
        ApplicationConfiguration.Initialize();

        var store = new SecureConfigStore();
        var config = AppConfig.CreateDefault();
        string? password = null;
        var loadedFromDisk = false;

        while (store.Exists)
        {
            using var passwordForm = new PasswordForm();
            var result = passwordForm.ShowDialog();
            if (result == DialogResult.Cancel)
                return;

            if (result == DialogResult.Retry)
            {
                var reset = MessageBox.Show(
                    $"{K("6riw7KG0IOyEpOygleydhCDsgq3soJztlZjqs6Ag7IOIIOyEpOygleydhCDsi5zsnpHtlaDquYzsmpQ/")}\n\n{K("7KCA7J6l65CcIFdlYmhvb2sgVVJMLCBBQ1Qg7ISk7KCVLCDssYTtjIUg7ZWE7YSwLCDrs7TslYgg7ISk7KCV7J20IOyCreygnOuQqeuLiOuLpC4=")}\n{K("7IKt7KCc65CcIOyEpOygleydgCDrs7XqtaztlaAg7IiYIOyXhuyKteuLiOuLpC4=")}",
                    K("7ISk7KCVIOy0iOq4sO2ZlA=="),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (reset == DialogResult.Yes)
                {
                    store.Delete();
                    config = AppConfig.CreateDefault();
                    password = null;
                    loadedFromDisk = false;
                    break;
                }

                continue;
            }

            try
            {
                password = passwordForm.Password;
                config = store.Load(password);
                loadedFromDisk = true;
                break;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{K("7ISk7KCV7J2EIOu2iOufrOyYpOyngCDrqrvtlojsirXri4jri6Qu")}\n\n{ex.Message}", K("67mE67CA67KI7Zi4IOuYkOuKlCDshKTsoJUg7Jik66WY"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        var mainForm = new MainForm(store, config, password, loadedFromDisk);
        singleInstanceSignal.ShowRequested = mainForm.ShowSettingsFromOtherInstance;
        if (singleInstanceSignal.ConsumePendingShowRequests())
            mainForm.ShowSettingsFromOtherInstance();

        Application.Run(mainForm);
    }
}
