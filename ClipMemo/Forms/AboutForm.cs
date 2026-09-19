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
        int plus = version.IndexOf('+');
        if (plus > 0) version = version[..plus];

        Text = "About ClipMemo";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(400, 300);
        MinimumSize = new Size(400, 300);
        BackColor = Color.FromArgb(28, 28, 32);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9f);
        Icon = AppIcon.Load();
        Padding = new Padding(24);

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
            Location = new Point(88, 54),
            ForeColor = Color.Gray,
        };

        var body = new Label
        {
            Text = "Tray clipboard memo for Windows.\r\n"
                 + "Multi-item history with pin, edit, search,\r\n"
                 + "global hotkey, and Start with Windows.\r\n\r\n"
                 + "License: MIT",
            AutoSize = false,
            Location = new Point(24, 96),
            Size = new Size(352, 100),
            ForeColor = Color.WhiteSmoke,
        };

        var link = new LinkLabel
        {
            Text = "github.com/donijokay/ClipMemo",
            AutoSize = true,
            Location = new Point(24, 210),
            LinkColor = Color.FromArgb(100, 180, 255),
            ActiveLinkColor = Color.White,
            VisitedLinkColor = Color.FromArgb(100, 180, 255),
        };
        link.Links.Clear();
        link.Links.Add(0, link.Text.Length, "https://github.com/donijokay/ClipMemo");
        link.LinkClicked += (_, e) =>
        {
            try
            {
                string url = e.Link?.LinkData as string ?? "https://github.com/donijokay/ClipMemo";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { /* ignore */ }
        };

        var ok = new Button
        {
            Text = "OK",
            Size = new Size(96, 32),
            Location = new Point(280, 248),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(64, 156, 255),
            ForeColor = Color.White,
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
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
