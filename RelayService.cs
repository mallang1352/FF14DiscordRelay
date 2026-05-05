using System.Security.Cryptography;
using System.Text;
using static FF14DiscordRelay.UiText;

namespace FF14DiscordRelay;

public enum RelayRuntimeState
{
    Running,
    PausedForAct,
}

public sealed class RelayService : IDisposable
{
    private readonly AppConfig _config;
    private readonly Action<string> _status;
    private readonly Action<RelayRuntimeState>? _stateChanged;
    private readonly Action<Exception>? _fatalError;
    private readonly Action<string>? _nicknameChanged;
    private readonly DiscordWebhookClient _webhook = new();
    private readonly HashSet<string> _enabledCodes;
    private readonly Dictionary<string, DateTimeOffset> _recentMessages = new(StringComparer.Ordinal);
    private readonly Queue<PendingRelayMessage> _pendingMessages = new();
    private readonly CancellationTokenSource _cts = new();
    private System.Threading.Timer? _timer;
    private string? _currentFile;
    private long _position;
    private int _busy;
    private bool _pausedForAct;
    private DateTimeOffset _rateLimitedUntil = DateTimeOffset.MinValue;

    public RelayService(AppConfig config, Action<string> status, Action<RelayRuntimeState>? stateChanged = null, Action<Exception>? fatalError = null, Action<string>? nicknameChanged = null)
    {
        _config = config;
        _status = status;
        _stateChanged = stateChanged;
        _fatalError = fatalError;
        _nicknameChanged = nicknameChanged;
        _enabledCodes = config.EnabledChannelCodes.Select(ChannelCatalog.NormalizeCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public void Start()
    {
        _stateChanged?.Invoke(RelayRuntimeState.Running);
        DetectCurrentNicknameFromLogs();
        _currentFile = FindNewestLogFile();
        if (_currentFile is not null)
        {
            _position = new FileInfo(_currentFile).Length;
            _status($"{K("QUNUIOuhnOq3uCDqsJDsi5wg7Iuc7J6ROiA=")}{Path.GetFileName(_currentFile)}");
        }
        else
        {
            _status(K("QUNUIOuhnOq3uCDtjIzsnbzsnYQg7JWE7KeBIOywvuyngCDrqrvtlojsirXri4jri6QuIO2PtOuNlOulvCDqsJDsi5ztlanri4jri6Qu"));
        }

        _timer = new System.Threading.Timer(async _ => await PollAsync(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(800));
    }

    public void Stop()
    {
        _cts.Cancel();
        _timer?.Dispose();
        _status(K("7KSR6rOE66W8IOykkeyngO2WiOyKteuLiOuLpC4="));
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }

    private async Task PollAsync()
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            return;

        try
        {
            if (!ProcessHelper.IsActRunning(_config.ActProcessNames))
            {
                PauseForActExit();
                return;
            }

            ResumeFromActRestart();

            var newest = FindNewestLogFile();
            if (newest is null)
                return;

            if (!string.Equals(newest, _currentFile, StringComparison.OrdinalIgnoreCase))
            {
                _currentFile = newest;
                _position = 0;
                _status($"{K("7IOIIOuhnOq3uCDtjIzsnbwg6rCQ7KeAOiA=")}{Path.GetFileName(newest)}");
            }

            await ReadNewLinesAsync(newest, _cts.Token);
            await FlushPendingMessagesAsync(_cts.Token);
            PruneDuplicates();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (DiscordWebhookClient.IsMissingWebhook(ex))
        {
            _status($"{K("7KSR6rOEIOyYpOulmDog")}{ex.Message}");
            _fatalError?.Invoke(ex);
        }
        catch (Exception ex)
        {
            _status($"{K("7KSR6rOEIOyYpOulmDog")}{ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    private void PauseForActExit()
    {
        if (_pausedForAct)
            return;

        _pausedForAct = true;
        _stateChanged?.Invoke(RelayRuntimeState.PausedForAct);
        _status(K("QUNUIOyiheujjCDqsJDsp4A6IOykkeqzhOulvCDsnbzsi5zsoJXsp4Dtlanri4jri6Qu"));
    }

    private void ResumeFromActRestart()
    {
        if (!_pausedForAct)
            return;

        _pausedForAct = false;
        _stateChanged?.Invoke(RelayRuntimeState.Running);
        _status(K("QUNUIOyerOyLpO2WiSDqsJDsp4A6IOykkeqzhOulvCDsnqzqsJztlanri4jri6Qu"));
    }

    private async Task ReadNewLinesAsync(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (info.Length < _position)
            _position = 0;
        if (info.Length == _position)
            return;

        string text;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            stream.Seek(_position, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            text = await reader.ReadToEndAsync(cancellationToken);
            _position = stream.Length;
        }

        foreach (var line in text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            DetectCurrentNicknameFromLine(line);

            var message = ActLogParser.TryParse(line);
            if (message is null)
                continue;
            if (!_enabledCodes.Contains(message.CodeHex))
                continue;
            if (_config.OwnMessagesOnly && !message.Sender.Equals(_config.Nickname, StringComparison.OrdinalIgnoreCase))
                continue;
            if (IsDuplicate(message))
                continue;

            var rendered = RenderMessage(message);
            _pendingMessages.Enqueue(new PendingRelayMessage(rendered, $"{K("7KCE7IahOiA=")}{message.ChannelName} / {message.Sender}: {message.Text}"));
        }
    }

    private async Task FlushPendingMessagesAsync(CancellationToken cancellationToken)
    {
        if (_pendingMessages.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        if (_rateLimitedUntil > now)
            return;

        while (_pendingMessages.Count > 0)
        {
            var batch = DequeueNextBatch();
            var content = string.Join(Environment.NewLine, batch.Select(x => x.Rendered));

            try
            {
                await _webhook.SendAsync(_config, content, cancellationToken);
            }
            catch (DiscordRateLimitException ex)
            {
                RequeueFront(batch);
                var waitSeconds = ex.RetryAfterSeconds + 0.5;
                _rateLimitedUntil = DateTimeOffset.UtcNow.AddSeconds(waitSeconds);
                _status($"{K("RGlzY29yZCDsoJztlZwg6rCQ7KeAOiA=")}{waitSeconds:0.#}{K("7LSIIO2bhCDrsIDrprAg66mU7Iuc7KeA66W8IOustuyWtCDsoITshqHtlanri4jri6Qu")}");
                return;
            }

            foreach (var message in batch)
                WriteLocalRelayLog(message.Rendered);

            if (batch.Count == 1)
                _status(batch[0].StatusLine);
            else
                _status($"{K("67CA66awIOuplOyLnOyngCDsoITshqE6IA==")}{batch.Count}{K("6rG0")}");
        }
    }

    private List<PendingRelayMessage> DequeueNextBatch()
    {
        const int maxBatchLength = 1900;
        var batch = new List<PendingRelayMessage>();
        var length = 0;

        while (_pendingMessages.Count > 0)
        {
            var next = _pendingMessages.Peek();
            var separatorLength = batch.Count == 0 ? 0 : Environment.NewLine.Length;
            var nextLength = next.Rendered.Length + separatorLength;

            if (batch.Count > 0 && length + nextLength > maxBatchLength)
                break;

            batch.Add(_pendingMessages.Dequeue());
            length += nextLength;

            if (next.Rendered.Length >= maxBatchLength)
                break;
        }

        return batch;
    }

    private void RequeueFront(IReadOnlyList<PendingRelayMessage> messages)
    {
        var rest = _pendingMessages.ToArray();
        _pendingMessages.Clear();

        foreach (var message in messages)
            _pendingMessages.Enqueue(message);
        foreach (var message in rest)
            _pendingMessages.Enqueue(message);
    }

    private bool IsDuplicate(ChatMessage message)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{message.CodeHex}\n{message.Sender}\n{message.Text}")));
        if (_recentMessages.ContainsKey(key))
            return true;

        _recentMessages[key] = DateTimeOffset.UtcNow;
        return false;
    }

    private void PruneDuplicates()
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-Math.Max(1, _config.DuplicateWindowSeconds));
        foreach (var key in _recentMessages.Where(x => x.Value < cutoff).Select(x => x.Key).ToArray())
            _recentMessages.Remove(key);
    }

    private string RenderMessage(ChatMessage message)
    {
        return _config.MessageTemplate
            .Replace("{channel}", message.ChannelName, StringComparison.OrdinalIgnoreCase)
            .Replace("{code}", message.CodeHex, StringComparison.OrdinalIgnoreCase)
            .Replace("{name}", message.Sender, StringComparison.OrdinalIgnoreCase)
            .Replace("{message}", message.Text, StringComparison.OrdinalIgnoreCase);
    }

    private string? FindNewestLogFile()
    {
        if (!Directory.Exists(_config.ActLogFolder))
            return null;

        return Directory.GetFiles(_config.ActLogFolder, "*.log", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private void DetectCurrentNicknameFromLogs()
    {
        var nickname = ActLogParser.DetectLatestPrimaryPlayerName(_config.ActLogFolder);
        if (!string.IsNullOrWhiteSpace(nickname))
            ApplyDetectedNickname(nickname);
    }

    private void DetectCurrentNicknameFromLine(string line)
    {
        var nickname = ActLogParser.TryParsePrimaryPlayerName(line);
        if (!string.IsNullOrWhiteSpace(nickname))
            ApplyDetectedNickname(nickname);
    }

    private void ApplyDetectedNickname(string nickname)
    {
        if (_config.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase))
            return;

        _config.Nickname = nickname;
        _nicknameChanged?.Invoke(nickname);
    }

    private void WriteLocalRelayLog(string rendered)
    {
        if (!_config.SaveLocalRelayLog)
            return;

        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, $"relay-{DateTime.Now:yyyyMMdd}.log"), $"[{DateTime.Now:HH:mm:ss}] {rendered}{Environment.NewLine}", Encoding.UTF8);
        }
        catch
        {
            // Local logs are optional and should never stop relay delivery.
        }
    }

    private sealed record PendingRelayMessage(string Rendered, string StatusLine);
}
