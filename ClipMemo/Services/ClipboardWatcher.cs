namespace ClipMemo.Services;

/// <summary>Polls the clipboard on a WinForms timer (~0.5s).</summary>
public sealed class ClipboardWatcher : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Action<string> _onNewText;
    private string? _lastSeen;
    private DateTime _suppressUntil = DateTime.MinValue;
    private readonly object _lock = new();
    private bool _disposed;

    public ClipboardWatcher(Action<string> onNewText, int pollIntervalMs = 500)
    {
        _onNewText = onNewText;
        _timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Clamp(pollIntervalMs, 200, 5000),
        };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        try
        {
            _lastSeen = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch
        {
            _lastSeen = null;
        }
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void NotifyCopied(string text)
    {
        lock (_lock)
        {
            _lastSeen = text;
            _suppressUntil = DateTime.UtcNow.AddSeconds(1);
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            if (!Clipboard.ContainsText(TextDataFormat.UnicodeText) &&
                !Clipboard.ContainsText(TextDataFormat.Text))
                return;

            string text;
            try
            {
                text = Clipboard.GetText(TextDataFormat.UnicodeText);
                if (string.IsNullOrEmpty(text))
                    text = Clipboard.GetText();
            }
            catch
            {
                return;
            }

            lock (_lock)
            {
                if (DateTime.UtcNow < _suppressUntil)
                {
                    _lastSeen = text;
                    return;
                }

                if (text == _lastSeen)
                    return;
                _lastSeen = text;
            }

            if (string.IsNullOrWhiteSpace(text))
                return;

            int bytes = System.Text.Encoding.UTF8.GetByteCount(text);
            if (bytes > MemoStore.MaxTextBytes)
                return;

            _onNewText(text);
        }
        catch
        {
            /* ignore clipboard races */
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }
}
