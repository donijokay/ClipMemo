using System.Text.Json.Serialization;

namespace ClipMemo.Models;

public sealed class AppSettings
{
    [JsonPropertyName("autostart")]
    public bool Autostart { get; set; }

    [JsonPropertyName("hotkey_modifiers")]
    public int HotkeyModifiers { get; set; } = Native.NativeMethods.MOD_CONTROL | Native.NativeMethods.MOD_SHIFT;

    [JsonPropertyName("hotkey_vk")]
    public int HotkeyVk { get; set; } = 0x56; // 'V'
}
