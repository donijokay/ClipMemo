using ClipMemo.Models;
using ClipMemo.Native;
using ClipMemo.Services;

namespace ClipMemo.Forms;

public sealed class SettingsForm : Form
{
    private readonly CheckBox _ctrl;
    private readonly CheckBox _shift;
    private readonly CheckBox _alt;
    private readonly CheckBox _win;
    private readonly CheckBox _autostart;
    private readonly TextBox _keyBox;
    private readonly Label _preview;
    private int _vk;

    public AppSettings ResultSettings { get; private set; }

    public SettingsForm(AppSettings current)
    {
        ResultSettings = new AppSettings
        {
            Autostart = current.Autostart,
            HotkeyModifiers = current.HotkeyModifiers,
            HotkeyVk = current.HotkeyVk,
        };
        _vk = current.HotkeyVk <= 0 ? 0x56 : current.HotkeyVk;

        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(380, 280);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9f);

        var title = new Label
        {
            Text = "Shortcut",
            AutoSize = true,
            Location = new Point(16, 16),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White,
        };

        _ctrl = MkCheck("Ctrl", 16, 52, (current.HotkeyModifiers & NativeMethods.MOD_CONTROL) != 0);
        _shift = MkCheck("Shift", 90, 52, (current.HotkeyModifiers & NativeMethods.MOD_SHIFT) != 0);
        _alt = MkCheck("Alt", 170, 52, (current.HotkeyModifiers & NativeMethods.MOD_ALT) != 0);
        _win = MkCheck("Win", 240, 52, (current.HotkeyModifiers & NativeMethods.MOD_WIN) != 0);

        var keyLabel = new Label
        {
            Text = "Key",
            AutoSize = true,
            Location = new Point(16, 96),
            ForeColor = Color.WhiteSmoke,
        };

        _keyBox = new TextBox
        {
            Location = new Point(16, 118),
            Size = new Size(340, 28),
            ReadOnly = true,
            Text = "Click here, then press a key…",
            BackColor = Color.FromArgb(45, 45, 50),
            ForeColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _keyBox.Enter += (_, _) => { _keyBox.Text = "Press a key…"; };
        _keyBox.KeyDown += OnKeyCapture;
        _keyBox.PreviewKeyDown += (_, e) => e.IsInputKey = true;

        _preview = new Label
        {
            AutoSize = true,
            Location = new Point(16, 156),
            ForeColor = Color.FromArgb(64, 156, 255),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
        };

        _autostart = MkCheck("Start with Windows", 16, 190, current.Autostart);

        var btnSave = new Button
        {
            Text = "Save",
            Size = new Size(88, 30),
            Location = new Point(180, 232),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(64, 156, 255),
            ForeColor = Color.White,
        };
        btnSave.Click += OnSave;

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(88, 30),
            Location = new Point(276, 232),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 66),
            ForeColor = Color.WhiteSmoke,
        };

        foreach (var c in new Control[] { _ctrl, _shift, _alt, _win })
            c.CheckedChanged += (_, _) => RefreshPreview();

        Controls.Add(title);
        Controls.Add(_ctrl);
        Controls.Add(_shift);
        Controls.Add(_alt);
        Controls.Add(_win);
        Controls.Add(keyLabel);
        Controls.Add(_keyBox);
        Controls.Add(_preview);
        Controls.Add(_autostart);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
        CancelButton = btnCancel;

        RefreshPreview();
        _keyBox.Text = HotkeyFormatter.VkName(_vk);
    }

    private static CheckBox MkCheck(string text, int x, int y, bool on) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        Checked = on,
        ForeColor = Color.WhiteSmoke,
        BackColor = Color.Transparent,
    };

    private int CurrentModifiers()
    {
        int m = 0;
        if (_ctrl.Checked) m |= NativeMethods.MOD_CONTROL;
        if (_shift.Checked) m |= NativeMethods.MOD_SHIFT;
        if (_alt.Checked) m |= NativeMethods.MOD_ALT;
        if (_win.Checked) m |= NativeMethods.MOD_WIN;
        return m;
    }

    private void RefreshPreview()
    {
        _preview.Text = "Current: " + HotkeyFormatter.Format(CurrentModifiers(), _vk);
    }

    private void OnKeyCapture(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        // Ignore pure modifiers
        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            return;

        int vk = (int)e.KeyCode;
        // Prefer letter/digit/F-keys; map A-Z from Keys enum values
        if (vk is >= 0x08 and <= 0xFE)
        {
            _vk = vk;
            _keyBox.Text = HotkeyFormatter.VkName(_vk);
            RefreshPreview();
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        int mods = CurrentModifiers();
        if (mods == 0)
        {
            MessageBox.Show(this, "Choose at least one modifier (Ctrl, Shift, Alt, or Win).", "Settings",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_vk <= 0)
        {
            MessageBox.Show(this, "Press a key for the shortcut.", "Settings",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Probe RegisterHotKey with a temporary hidden window
        using var probe = new Form { ShowInTaskbar = false, Opacity = 0, Width = 0, Height = 0 };
        probe.Show();
        probe.Hide();
        const int probeId = 0xC11E;
        uint regMods = (uint)mods | (uint)NativeMethods.MOD_NOREPEAT;
        bool ok = NativeMethods.RegisterHotKey(probe.Handle, probeId, regMods, (uint)_vk);
        if (ok)
            NativeMethods.UnregisterHotKey(probe.Handle, probeId);
        if (!ok)
        {
            MessageBox.Show(this,
                "That shortcut is already in use by another app. Pick a different combination.",
                "Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ResultSettings = new AppSettings
        {
            Autostart = _autostart.Checked,
            HotkeyModifiers = mods,
            HotkeyVk = _vk,
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
