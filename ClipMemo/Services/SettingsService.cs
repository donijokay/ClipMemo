using System.Text;
using System.Text.Json;
using ClipMemo.Models;

namespace ClipMemo.Services;

public sealed class SettingsService
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public SettingsService(string? dataDir = null)
    {
        string dir = dataDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipMemo");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new AppSettings();
            string raw = File.ReadAllText(_path, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppSettings>(raw, JsonOpts) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, JsonOpts);
        File.WriteAllText(_path, json, new UTF8Encoding(false));
    }
}
