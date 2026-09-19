using ClipMemo.Models;
using ClipMemo.Native;
using ClipMemo.Services;

namespace ClipMemo.Forms;

/// <summary>Dark compact clipboard history panel.</summary>
public sealed class MainForm : Form
{
    private const int PanelWidth = 380;
    private const int PanelHeight = 500;
    private const int PreviewLen = 100;

    private readonly MemoStore _store;
    private readonly ClipboardWatcher _watcher;
    private readonly Action? _onCloseRequest;
    private HotkeyService? _hotkey;
    private int _hotkeyMods;
    private int _hotkeyVk;
    private Label? _hotkeyHint;
    private bool _allowClose;

    private readonly TextBox _search;
    private readonly Label _status;
    private readonly Panel _listHost;
    private readonly FlowLayoutPanel _list;
    private readonly Label _emptyLabel;
    private System.Windows.Forms.Timer? _statusTimer;

    public MainForm(MemoStore store, ClipboardWatcher watcher, Action? onCloseRequest = null)
    {
        _hotkeyMods = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;
        _hotkeyVk = 0x56;
        _store = store;
        _watcher = watcher;
        _onCloseRequest = onCloseRequest;

        Text = "ClipMemo";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(PanelWidth, PanelHeight);
        MinimumSize = new Size(PanelWidth, 360);
        BackColor = Color.FromArgb(28, 28, 32);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9f);
        Icon = AppIcon.Load();

        // Header
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(12, 8, 12, 4),
            BackColor = Color.FromArgb(28, 28, 32),
        };

        var badge = new Label
        {
            Text = "CM",
            Size = new Size(28, 28),
            Location = new Point(12, 6),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(64, 156, 255),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };

        var title = new Label
        {
            Text = "ClipMemo",
            AutoSize = true,
            Location = new Point(48, 10),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
        };

        _hotkeyHint = new Label
        {
            Text = "Ctrl+Shift+V",
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f),
        };
        _hotkeyHint.Location = new Point(PanelWidth - 130, 12);
        var hotkeyHint = _hotkeyHint;

        header.Controls.Add(badge);
        header.Controls.Add(title);
        header.Controls.Add(_hotkeyHint!);

        // Search
        var searchHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(12, 4, 12, 2),
        };
        _search = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search…",
            BackColor = Color.FromArgb(45, 45, 50),
            ForeColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f),
        };
        _search.TextChanged += (_, _) => RefreshList();
        searchHost.Controls.Add(_search);

        // Status
        _status = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(14, 0, 12, 0),
            Text = "",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        // List
        _listHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 4, 10, 6),
            BackColor = Color.FromArgb(28, 28, 32),
        };

        _list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.FromArgb(36, 36, 40),
            Padding = new Padding(4),
        };

        _emptyLabel = new Label
        {
            Text = "Copy any text — it will show up here.",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 10f),
            Visible = false,
        };

        _listHost.Controls.Add(_list);
        _listHost.Controls.Add(_emptyLabel);

        // Footer
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(12, 6, 12, 8),
        };

        var btnClear = new Button
        {
            Text = "Clear",
            Size = new Size(96, 28),
            Location = new Point(12, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.WhiteSmoke,
        };
        btnClear.FlatAppearance.BorderSize = 0;
        btnClear.Click += (_, _) => ClearUnpinned();

        var btnHide = new Button
        {
            Text = "Hide",
            Size = new Size(110, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(64, 156, 255),
            ForeColor = Color.White,
        };
        btnHide.FlatAppearance.BorderSize = 0;
        btnHide.Location = new Point(ClientSize.Width - 12 - 110, 8);
        btnHide.Click += (_, _) => HideToTray();

        footer.Controls.Add(btnClear);
        footer.Controls.Add(btnHide);
        footer.Resize += (_, _) =>
        {
            btnHide.Location = new Point(footer.ClientSize.Width - 12 - btnHide.Width, 8);
        };

        Controls.Add(_listHost);
        Controls.Add(_status);
        Controls.Add(searchHost);
        Controls.Add(header);
        Controls.Add(footer);

        HandleCreated += OnHandleCreated;
        FormClosing += OnFormClosing;
        PositionNearTray();
        RefreshList();
    }

    /// <summary>Force HWND creation so global hotkey can register before first Show.</summary>
    public void EnsureHandle()
    {
        _ = Handle;
    }

    private void OnHandleCreated(object? sender, EventArgs e)
    {
        ApplyHotkey(_hotkeyMods != 0 ? _hotkeyMods : (NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT),
            _hotkeyVk != 0 ? _hotkeyVk : 0x56);
    }

    /// <summary>Register or re-register the global shortcut.</summary>
    public bool ApplyHotkey(int modifiers, int vk)
    {
        _hotkeyMods = modifiers;
        _hotkeyVk = vk;
        if (!IsHandleCreated)
        {
            EnsureHandle();
        }
        _hotkey?.Dispose();
        _hotkey = new HotkeyService(Handle);
        bool ok = _hotkey.Register((uint)modifiers, (uint)vk);
        UpdateHotkeyHint();
        return ok;
    }

    public void UpdateHotkeyHint()
    {
        string text = HotkeyFormatter.Format(
            _hotkeyMods != 0 ? _hotkeyMods : (NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT),
            _hotkeyVk != 0 ? _hotkeyVk : 0x56);
        if (_hotkeyHint is not null)
            _hotkeyHint.Text = text;
        // also update any header label named similarly
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
            return;
        if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.ApplicationExitCall)
        {
            e.Cancel = true;
            HideToTray();
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyService.HotkeyId)
        {
            TogglePanel();
            return;
        }
        base.WndProc(ref m);
    }

    public void ShowPanel()
    {
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        PositionNearTray();
        Show();
        Activate();
        BringToFront();
        TopMost = true;
        TopMost = false;
        _search.Focus();
        RefreshList();
    }

    public void HideToTray()
    {
        Hide();
    }

    public void TogglePanel()
    {
        if (Visible && WindowState != FormWindowState.Minimized)
            HideToTray();
        else
            ShowPanel();
    }

    public void RefreshList()
    {
        string query = _search.Text.Trim();
        var memos = _store.ListMemos(query);

        _list.SuspendLayout();
        _list.Controls.Clear();

        if (memos.Count == 0)
        {
            _list.Visible = false;
            _emptyLabel.Visible = true;
            _emptyLabel.Text = string.IsNullOrEmpty(query)
                ? "Copy any text — it will show up here."
                : "No results.";
            _emptyLabel.BringToFront();
        }
        else
        {
            _emptyLabel.Visible = false;
            _list.Visible = true;
            int rowWidth = Math.Max(200, _list.ClientSize.Width - 24);
            foreach (var memo in memos)
            {
                var row = CreateRow(memo, rowWidth);
                _list.Controls.Add(row);
            }
        }

        _list.ResumeLayout();
        SetStatus($"{_store.ListMemos().Count} memos");
    }

    private Control CreateRow(Memo memo, int width)
    {
        var row = new Panel
        {
            Width = width,
            Height = 36,
            Margin = new Padding(2),
            BackColor = Color.FromArgb(48, 48, 54),
            Cursor = Cursors.Hand,
            Tag = memo,
        };

        string preview = memo.Text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (preview.Length > PreviewLen)
            preview = preview[..(PreviewLen - 1)] + "…";

        var lbl = new Label
        {
            Text = preview,
            AutoSize = false,
            Location = new Point(8, 0),
            Size = new Size(width - 110, 36),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.WhiteSmoke,
            Cursor = Cursors.Hand,
            Tag = memo,
        };
        lbl.Click += (_, _) => CopyMemo(memo);
        row.Click += (_, _) => CopyMemo(memo);

        var btnPin = MakeIconButton(memo.Pinned ? "📌" : "○", width - 100, memo.Pinned ? "Unpin" : "Pin");
        btnPin.Click += (_, _) =>
        {
            bool wasPinned = memo.Pinned;
            _store.SetPinned(memo.Id, !memo.Pinned);
            RefreshList();
            FlashStatus(wasPinned ? "Unpinned." : "Pinned.");
        };

        var btnEdit = MakeIconButton("✎", width - 68, "Edit");
        btnEdit.Click += (_, _) => EditMemo(memo);

        var btnDel = MakeIconButton("✕", width - 36, "Delete");
        btnDel.ForeColor = Color.FromArgb(224, 112, 112);
        btnDel.Click += (_, _) =>
        {
            _store.Delete(memo.Id);
            RefreshList();
            FlashStatus("Deleted.");
        };

        row.Controls.Add(lbl);
        row.Controls.Add(btnPin);
        row.Controls.Add(btnEdit);
        row.Controls.Add(btnDel);

        btnPin.MouseEnter += (_, _) => SetStatus(memo.Pinned ? "Unpin" : "Pin");
        btnEdit.MouseEnter += (_, _) => SetStatus("Edit");
        btnDel.MouseEnter += (_, _) => SetStatus("Delete");
        lbl.MouseEnter += (_, _) => SetStatus("Click to copy");

        return row;
    }

    private static Button MakeIconButton(string text, int x, string tip)
    {
        var b = new Button
        {
            Text = text,
            Size = new Size(28, 28),
            Location = new Point(x, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.WhiteSmoke,
            Cursor = Cursors.Hand,
            TabStop = false,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 78);
        return b;
    }

    private void CopyMemo(Memo memo)
    {
        try
        {
            Clipboard.SetText(memo.Text);
            _watcher.NotifyCopied(memo.Text);
            FlashStatus("Copied.");
        }
        catch
        {
            FlashStatus("Copy failed.");
        }
    }

    private void EditMemo(Memo memo)
    {
        using var dlg = new EditMemoForm(memo.Text);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            string text = dlg.EditedText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                _store.UpdateText(memo.Id, text);
                RefreshList();
            }
        }
    }

    private void ClearUnpinned()
    {
        int n = _store.ClearUnpinned();
        RefreshList();
        FlashStatus(n > 0 ? $"Cleared · {n} removed." : "Nothing to clear.");
    }

    private void SetStatus(string msg) => _status.Text = msg;

    private void FlashStatus(string msg)
    {
        SetStatus(msg);
        _statusTimer?.Stop();
        _statusTimer?.Dispose();
        _statusTimer = new System.Windows.Forms.Timer { Interval = 1800 };
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            SetStatus($"{_store.ListMemos().Count} memos");
        };
        _statusTimer.Start();
    }

    private void PositionNearTray()
    {
        var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Left = wa.Right - Width - 16;
        Top = wa.Bottom - Height - 16;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkey?.Dispose();
            _statusTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
