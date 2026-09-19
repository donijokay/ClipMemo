using System.Diagnostics;
using System.Reflection;

namespace ClipMemo.Forms;

/// <summary>Simple About dialog matching the dark ClipMemo theme.</summary>
public sealed class AboutForm : Form
{
    public AboutForm()
    {
        string version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "2.0.0";
        // trim git hash if present
        int plus = version.IndexOf('+');
        if (plus > 0) version = version[..plus];

        Text = "About ClipMemo";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(360, 260);
        BackColor = Color.FromArgb(28, 28, 32);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9f);
        Icon = AppIcon.Load();

        var logo = new PictureBox
        {
            Image = AppIcon.GetLogoBitmap(48),
            Size = new Size(48, 48),
            Location = new Point(24, 24),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };

        var title = new Label
        {
            Text = "ClipMemo",
            AutoSize = true,
            Location = new Point(88, 24),
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.White,
        };

        var ver = new Label
        {
            Text = "Version " + version,
            AutoSize = true,
            Location = new Point(88, 52),
            ForeColor = Color.Gray,
        };

        var body = new Label
        {
            Text = "Tray clipboard memo for Windows.\n"
                 + "Multi-item history with pin, edit, search,\n"
                 + "global hotkey, and Start with Windows.\n\n"
                 + "License: MIT",
            AutoSize = true,
            Location = new Point(24, 92),
            ForeColor = Color.WhiteSmoke,
        };

        var link = new LinkLabel
        {
            Text = "github.com/donijokay/ClipMemo",
            AutoSize = true,
            Location = new Point(24, 178),
            LinkColor = Color.FromArgb(100, 180, 255),
            ActiveLinkColor = Color.White,
            VisitedLinkColor = Color.FromArgb(100, 180, 255),
        };
        link.LinkClicked += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/donijokay/ClipMemo",
                    UseShellExecute = true,
                });
            }
            catch { /* ignore */ }
        };

        var ok = new Button
        {
            Text = "OK",
            Size = new Size(88, 30),
            Location = new Point(248, 210),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(64, 156, 255),
            ForeColor = Color.White,
            DialogResult = DialogResult.OK,
        };
        ok.FlatAppearance.BorderSize = 0;
        AcceptButton = ok;

        Controls.Add(logo);
        Controls.Add(title);
        Controls.Add(ver);
        Controls.Add(body);
        Controls.Add(link);
        Controls.Add(ok);
    }

    public static void ShowAbout(IWin32Window? owner)
    {
        using var dlg = new AboutForm();
        if (owner is not null)
            dlg.ShowDialog(owner);
        else
            dlg.ShowDialog();
    }
}
