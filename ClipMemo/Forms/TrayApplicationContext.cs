using ClipMemo.Models;
using ClipMemo.Services;

namespace ClipMemo.Forms;

/// <summary>System-tray host: clipboard watch, panel, autostart, exit.</summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _autostartItem;
    private readonly MemoStore _store;
    private readonly SettingsService _settingsService;
    private readonly ClipboardWatcher _watcher;
    private readonly MainForm _form;
    private AppSettings _settings;

    public TrayApplicationContext()
    {
        _store = new MemoStore();
        _settingsService = new SettingsService(_store.DataDirectory);
        _settings = _settingsService.Load();

        try { StartupService.SetStartWithWindows(_settings.Autostart); }
        catch { /* ignore */ }

        _watcher = new ClipboardWatcher(OnNewClipboardText);
        _form = new MainForm(_store, _watcher);
        _form.EnsureHandle(); // register Ctrl+Shift+V before first show
        _form.Hide();

        _autostartItem = new ToolStripMenuItem("Start with Windows", null, OnToggleAutostart)
        {
            Checked = StartupService.IsStartWithWindowsEnabled() || _settings.Autostart,
            CheckOnClick = false,
        };

        _menu = new ContextMenuStrip();
        _menu.Items.Add(new ToolStripMenuItem("Open", null, OnOpen) { Font = new Font(SystemFonts.MenuFont!, FontStyle.Bold) });
        _menu.Items.Add(_autostartItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Exit", null, OnExit));

        _tray = new NotifyIcon
        {
            Icon = AppIcon.Load(),
            Text = "ClipMemo",
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _tray.DoubleClick += (_, _) => OnOpen(null!, EventArgs.Empty);

        _watcher.Start();

        try
        {
            _tray.BalloonTipTitle = "ClipMemo";
            _tray.BalloonTipText = "Ready — Ctrl+Shift+V or right-click the tray → Open.";
            _tray.BalloonTipIcon = ToolTipIcon.Info;
            _tray.ShowBalloonTip(2500);
        }
        catch { /* ignore */ }
    }

    private void OnNewClipboardText(string text)
    {
        try
        {
            var memo = _store.AddText(text);
            if (memo is null)
                return;

            if (_form.IsHandleCreated)
            {
                _form.BeginInvoke(new Action(() =>
                {
                    if (_form.Visible)
                        _form.RefreshList();
                }));
            }
        }
        catch
        {
            /* ignore */
        }
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        try
        {
            _form.ShowPanel();
        }
        catch
        {
            /* ignore */
        }
    }

    private void OnToggleAutostart(object? sender, EventArgs e)
    {
        try
        {
            bool next = !_autostartItem.Checked;
            StartupService.SetStartWithWindows(next);
            _settings.Autostart = next;
            _settingsService.Save(_settings);
            _autostartItem.Checked = StartupService.IsStartWithWindowsEnabled();
        }
        catch
        {
            _autostartItem.Checked = StartupService.IsStartWithWindowsEnabled();
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        try
        {
            _watcher.Stop();
            _watcher.Dispose();
        }
        catch { /* ignore */ }

        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();

        try
        {
            _form.ForceClose();
            _form.Dispose();
        }
        catch { /* ignore */ }

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher.Dispose();
            _tray.Dispose();
            _menu.Dispose();
            _form.Dispose();
        }
        base.Dispose(disposing);
    }
}
