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
    private readonly Panel _fullPreview;
    private readonly Label _fullPreviewLabel;
    private Control? _previewOwner;

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

        _fullPreviewLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Padding = new Padding(10, 8, 10, 8),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(40, 52, 72),
            Font = new Font("Segoe UI", 9.25f),
            TextAlign = ContentAlignment.TopLeft,
        };
        _fullPreview = new Panel
        {
            Visible = false,
            BackColor = Color.FromArgb(40, 52, 72),
            Padding = new Padding(1),
            BorderStyle = BorderStyle.FixedSingle,
        };
        _fullPreview.Controls.Add(_fullPreviewLabel);
        Controls.Add(_fullPreview);

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
        _fullPreview.Visible = false;
        _previewOwner = null;
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
        const int rowH = 58;
        const int btnY = 15; // vertically center 28px buttons in 58px row

        var row = new Panel
        {
            Width = width,
            Height = rowH,
            Margin = new Padding(2, 3, 2, 3),
            Padding = new Padding(4, 6, 4, 6),
            BackColor = Color.FromArgb(48, 48, 54),
            Cursor = Cursors.Hand,
            Tag = memo,
        };

        string flat = memo.Text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        bool truncated = flat.Length > PreviewLen
            || memo.Text.IndexOf('\n') >= 0
            || memo.Text.IndexOf('\r') >= 0;
        string preview = flat.Length > PreviewLen
            ? flat[..(PreviewLen - 1)] + "…"
            : flat;

        var lbl = new Label
        {
            Text = preview,
            AutoSize = false,
            Location = new Point(10, 6),
            Size = new Size(width - 118, rowH - 12),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.WhiteSmoke,
            Cursor = Cursors.Hand,
            Tag = memo,
            // Slightly larger type + padding so a full line is easy to read
            Font = new Font("Segoe UI", 9.5f),
        };

        // Also treat as truncated if the full one-line text is wider than the label
        if (!truncated && flat.Length > 0)
        {
            int textW = TextRenderer.MeasureText(flat, lbl.Font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            if (textW > lbl.Width)
                truncated = true;
        }
        lbl.Click += (_, _) => CopyMemo(memo);
        row.Click += (_, _) => CopyMemo(memo);

        var btnPin = MakeIconButton(memo.Pinned ? "📌" : "○", width - 100, btnY, memo.Pinned ? "Unpin" : "Pin");
        btnPin.Click += (_, _) =>
        {
            bool wasPinned = memo.Pinned;
            _store.SetPinned(memo.Id, !memo.Pinned);
            RefreshList();
            FlashStatus(wasPinned ? "Unpinned." : "Pinned.");
        };

        var btnEdit = MakeIconButton("✎", width - 68, btnY, "Edit");
        btnEdit.Click += (_, _) => EditMemo(memo);

        var btnDel = MakeIconButton("✕", width - 36, btnY, "Delete");
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

        // Soft hover highlight + in-app full-text preview (WinForms ToolTip often fails on tray forms)
        Color normalBg = Color.FromArgb(48, 48, 54);
        Color hoverBg = Color.FromArgb(62, 78, 104);

        void SetHover(bool on)
        {
            row.BackColor = on ? hoverBg : normalBg;
            lbl.BackColor = on ? hoverBg : normalBg;
            if (on)
            {
                // Full-text panel only when the row text is truncated / too long
                if (truncated)
                    ShowFullPreview(row, memo.Text);
                else
                    HideFullPreviewIfOwner(row);
            }
            else
                HideFullPreviewIfOwner(row);
        }

        void WireHover(Control c)
        {
            c.MouseEnter += (_, _) => SetHover(true);
            c.MouseLeave += (_, _) =>
            {
                var pt = row.PointToClient(Cursor.Position);
                if (!row.ClientRectangle.Contains(pt))
                    SetHover(false);
            };
        }

        WireHover(row);
        WireHover(lbl);
        WireHover(btnPin);
        WireHover(btnEdit);
        WireHover(btnDel);

        btnPin.MouseEnter += (_, _) => SetStatus(memo.Pinned ? "Unpin" : "Pin");
        btnEdit.MouseEnter += (_, _) => SetStatus("Edit");
        btnDel.MouseEnter += (_, _) => SetStatus("Delete");
        lbl.MouseEnter += (_, _) => SetStatus("Click to copy");

        return row;
    }

    private void ShowFullPreview(Control owner, string text)
    {
        _previewOwner = owner;
        string body = string.IsNullOrWhiteSpace(text) ? "(empty)" : text;
        if (body.Length > 4000)
            body = body[..4000] + "…";

        _fullPreviewLabel.Text = body;

        int maxW = Math.Max(220, ClientSize.Width - 24);
        var measured = TextRenderer.MeasureText(
            body,
            _fullPreviewLabel.Font,
            new Size(maxW - 24, 0),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        int h = Math.Min(220, Math.Max(40, measured.Height + 20));
        int w = Math.Min(maxW, Math.Max(200, measured.Width + 28));
        _fullPreview.Size = new Size(w, h);

        Point below = owner.PointToScreen(new Point(0, owner.Height + 2));
        Point local = PointToClient(below);
        int x = Math.Max(8, Math.Min(local.X, ClientSize.Width - _fullPreview.Width - 8));
        int y = local.Y;
        if (y + _fullPreview.Height > ClientSize.Height - 8)
            y = Math.Max(8, PointToClient(owner.PointToScreen(Point.Empty)).Y - _fullPreview.Height - 2);
        _fullPreview.Location = new Point(x, y);
        _fullPreview.Visible = true;
        _fullPreview.BringToFront();
    }

    private void HideFullPreviewIfOwner(Control owner)
    {
        if (_previewOwner == owner)
        {
            _fullPreview.Visible = false;
            _previewOwner = null;
        }
    }

    private static Button MakeIconButton(string text, int x, int y, string tip)
    {
        var b = new Button
        {
            Text = text,
            Size = new Size(28, 28),
            Location = new Point(x, y),
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
