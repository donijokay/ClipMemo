using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClipMemo.Models;

namespace ClipMemo.Services;

/// <summary>JSON-backed memo store under %APPDATA%\ClipMemo\memos.json.</summary>
public sealed class MemoStore
{
    public const int MaxUnpinned = 100;
    public const int MaxTextBytes = 100 * 1024;

    private readonly object _lock = new();
    private readonly string _dataDir;
    private readonly string _dataFile;
    private List<Memo> _memos = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MemoStore(string? dataDir = null)
    {
        _dataDir = dataDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipMemo");
        _dataFile = Path.Combine(_dataDir, "memos.json");
        Directory.CreateDirectory(_dataDir);
        Load();
    }

    public string DataDirectory => _dataDir;

    public void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_dataFile))
            {
                _memos = new List<Memo>();
                return;
            }

            try
            {
                string raw = File.ReadAllText(_dataFile, Encoding.UTF8);
                using var doc = JsonDocument.Parse(raw);
                JsonElement root = doc.RootElement;
                JsonElement items = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("memos", out var m)
                    ? m
                    : root;

                _memos = new List<Memo>();
                if (items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        _memos.Add(new Memo
                        {
                            Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                            Text = item.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "",
                            CreatedAt = item.TryGetProperty("created_at", out var c) ? c.GetString() ?? "" : "",
                            Pinned = item.TryGetProperty("pinned", out var p) && p.ValueKind == JsonValueKind.True,
                            UpdatedAt = item.TryGetProperty("updated_at", out var u) ? u.GetString() : null,
                        });
                    }
                }

                EnforceCap(save: false);
            }
            catch
            {
                _memos = new List<Memo>();
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            Directory.CreateDirectory(_dataDir);
            var payload = new MemoFile
            {
                Version = 1,
                Memos = _memos,
            };
            string json = JsonSerializer.Serialize(payload, JsonOpts);
            string tmp = _dataFile + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(tmp, _dataFile, overwrite: true);
        }
    }

    private List<Memo> Sorted()
    {
        var pinned = _memos.Where(m => m.Pinned).OrderByDescending(m => m.SortKey).ToList();
        var unpinned = _memos.Where(m => !m.Pinned).OrderByDescending(m => m.SortKey).ToList();
        return pinned.Concat(unpinned).ToList();
    }

    private void EnforceCap(bool save)
    {
        var pinned = _memos.Where(m => m.Pinned).ToList();
        var unpinned = _memos.Where(m => !m.Pinned).OrderByDescending(m => m.SortKey).ToList();
        if (unpinned.Count > MaxUnpinned)
            unpinned = unpinned.Take(MaxUnpinned).ToList();
        _memos = pinned.Concat(unpinned).ToList();
        if (save)
            SaveUnlocked();
    }

    private void SaveUnlocked()
    {
        Directory.CreateDirectory(_dataDir);
        var payload = new MemoFile { Version = 1, Memos = _memos };
        string json = JsonSerializer.Serialize(payload, JsonOpts);
        string tmp = _dataFile + ".tmp";
        File.WriteAllText(tmp, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(tmp, _dataFile, overwrite: true);
    }

    public List<Memo> ListMemos(string query = "")
    {
        lock (_lock)
        {
            var memos = Sorted();
            if (string.IsNullOrWhiteSpace(query))
                return memos;
            string q = query.Trim().ToLowerInvariant();
            return memos.Where(m => m.Text.ToLowerInvariant().Contains(q)).ToList();
        }
    }

    public Memo? Get(string memoId)
    {
        lock (_lock)
            return _memos.FirstOrDefault(m => m.Id == memoId);
    }

    public Memo? AddText(string text)
    {
        text ??= "";
        if (string.IsNullOrWhiteSpace(text))
            return null;
        int byteLen = Encoding.UTF8.GetByteCount(text);
        if (byteLen > MaxTextBytes)
            return null;

        lock (_lock)
        {
            if (_memos.Count > 0)
            {
                var latest = _memos.OrderByDescending(m => m.SortKey).First();
                if (latest.Text == text)
                    return null;
            }

            var memo = Memo.Create(text);
            _memos.Add(memo);
            EnforceCap(save: true);
            return memo;
        }
    }

    public bool UpdateText(string memoId, string text)
    {
        lock (_lock)
        {
            var m = _memos.FirstOrDefault(x => x.Id == memoId);
            if (m is null)
                return false;
            m.Text = text;
            m.UpdatedAt = DateTime.UtcNow.ToString("o");
            SaveUnlocked();
            return true;
        }
    }

    public bool SetPinned(string memoId, bool pinned)
    {
        lock (_lock)
        {
            var m = _memos.FirstOrDefault(x => x.Id == memoId);
            if (m is null)
                return false;
            m.Pinned = pinned;
            m.UpdatedAt = DateTime.UtcNow.ToString("o");
            EnforceCap(save: true);
            return true;
        }
    }

    public bool Delete(string memoId)
    {
        lock (_lock)
        {
            int before = _memos.Count;
            _memos = _memos.Where(m => m.Id != memoId).ToList();
            if (_memos.Count < before)
            {
                SaveUnlocked();
                return true;
            }
            return false;
        }
    }

    public int ClearUnpinned()
    {
        lock (_lock)
        {
            int before = _memos.Count;
            _memos = _memos.Where(m => m.Pinned).ToList();
            int removed = before - _memos.Count;
            if (removed > 0)
                SaveUnlocked();
            return removed;
        }
    }

    private sealed class MemoFile
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("memos")]
        public List<Memo> Memos { get; set; } = new();
    }
}
