using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static FF14DiscordRelay.UiText;

namespace FF14DiscordRelay;

public sealed class SecureConfigStore
{
    private const int SaltBytes = 16;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int KeyBytes = 32;
    private const int Iterations = 250_000;

    public string ConfigPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FF14DiscordRelay",
        "config.enc");

    private string LegacyConfigPath { get; } = Path.Combine(AppContext.BaseDirectory, "config.enc");

    public bool Exists => File.Exists(ConfigPath) || File.Exists(LegacyConfigPath);

    public AppConfig Load(string password)
    {
        var path = GetExistingConfigPath();
        var envelope = JsonSerializer.Deserialize<EncryptedEnvelope>(File.ReadAllText(path))
            ?? throw new InvalidOperationException(K("7ISk7KCVIO2MjOydvCDtmJXsi53snbQg7Jis67CU66W07KeAIOyViuyKteuLiOuLpC4="));

        var salt = Convert.FromBase64String(envelope.Salt);
        var nonce = Convert.FromBase64String(envelope.Nonce);
        var tag = Convert.FromBase64String(envelope.Tag);
        var cipher = Convert.FromBase64String(envelope.Ciphertext);
        var key = DeriveKey(password, salt, envelope.Iterations);
        var plain = new byte[cipher.Length];

        try
        {
            using var aes = new AesGcm(key, TagBytes);
            aes.Decrypt(nonce, cipher, tag, plain);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(K("67mE67CA67KI7Zi46rCAIO2LgOuguOqxsOuCmCDshKTsoJUg7YyM7J287J20IOyGkOyDgeuQmOyXiOyKteuLiOuLpC4="), ex);
        }

        var json = Encoding.UTF8.GetString(plain);
        var config = JsonSerializer.Deserialize<AppConfig>(json) ?? AppConfig.CreateDefault();
        if (!path.Equals(ConfigPath, StringComparison.OrdinalIgnoreCase))
            Save(config, password);
        return config;
    }

    public void Save(AppConfig config, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(K("67mE67CA67KI7Zi46rCAIOu5hOyWtCDsnojsirXri4jri6Qu"), nameof(password));

        var plain = JsonSerializer.SerializeToUtf8Bytes(config, new JsonSerializerOptions { WriteIndented = true });
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagBytes];
        var key = DeriveKey(password, salt, Iterations);

        using (var aes = new AesGcm(key, TagBytes))
            aes.Encrypt(nonce, plain, cipher, tag);

        var envelope = new EncryptedEnvelope
        {
            Version = 1,
            Kdf = "PBKDF2-SHA256",
            Iterations = Iterations,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Ciphertext = Convert.ToBase64String(cipher),
        };

        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true }));
        DeleteLegacyConfig();
    }

    public void Delete()
    {
        if (File.Exists(ConfigPath))
            File.Delete(ConfigPath);
        DeleteLegacyConfig();
    }

    private string GetExistingConfigPath()
    {
        if (File.Exists(ConfigPath))
            return ConfigPath;
        if (File.Exists(LegacyConfigPath))
            return LegacyConfigPath;

        return ConfigPath;
    }

    private void DeleteLegacyConfig()
    {
        if (!LegacyConfigPath.Equals(ConfigPath, StringComparison.OrdinalIgnoreCase) && File.Exists(LegacyConfigPath))
            File.Delete(LegacyConfigPath);
    }

    private static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeyBytes);
    }

    private sealed class EncryptedEnvelope
    {
        public int Version { get; set; }
        public string Kdf { get; set; } = "";
        public int Iterations { get; set; }
        public string Salt { get; set; } = "";
        public string Nonce { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Ciphertext { get; set; } = "";
    }
}
