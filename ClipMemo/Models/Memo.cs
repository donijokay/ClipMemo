using System.Text.Json.Serialization;

namespace ClipMemo.Models;

public sealed class Memo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("pinned")]
    public bool Pinned { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    public static Memo Create(string text, bool pinned = false)
    {
        string now = DateTime.UtcNow.ToString("o");
        return new Memo
        {
            Id = Guid.NewGuid().ToString(),
            Text = text,
            CreatedAt = now,
            Pinned = pinned,
            UpdatedAt = now,
        };
    }

    public string SortKey => UpdatedAt ?? CreatedAt;
}
