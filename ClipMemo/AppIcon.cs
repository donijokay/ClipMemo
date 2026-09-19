namespace ClipMemo;

/// <summary>
/// Loads clipmemo.ico from Assets next to the exe, or draws a rounded blue "CM" badge.
/// </summary>
internal static class AppIcon
{
    private static Icon? _cached;

    public static Icon Load()
    {
        if (_cached is not null)
            return _cached;

        try
        {
            string icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "clipmemo.ico");
            if (File.Exists(icoPath))
            {
                using var icon = new Icon(icoPath);
                _cached = (Icon)icon.Clone();
                return _cached;
            }
        }
        catch
        {
            /* fall through */
        }

        try
        {
            _cached = CreateCmIcon(32);
            return _cached;
        }
        catch
        {
            /* fall through */
        }

        try
        {
            string? exe = Environment.ProcessPath ?? Application.ExecutablePath;
            if (!string.IsNullOrEmpty(exe))
            {
                var extracted = Icon.ExtractAssociatedIcon(exe);
                if (extracted is not null)
                {
                    _cached = extracted;
                    return _cached;
                }
            }
        }
        catch
        {
            /* fall through */
        }

        _cached = (Icon)SystemIcons.Application.Clone();
        return _cached;
    }


    /// <summary>Bitmap logo matching the tray icon (prefers Assets/clipmemo.png).</summary>
    public static Bitmap GetLogoBitmap(int size)
    {
        try
        {
            string pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "clipmemo.png");
            if (File.Exists(pngPath))
            {
                using var src = new Bitmap(pngPath);
                return new Bitmap(src, new Size(size, size));
            }
        }
        catch { /* fall through */ }

        try
        {
            using var icon = Load();
            using var fromIcon = icon.ToBitmap();
            return new Bitmap(fromIcon, new Size(size, size));
        }
        catch { /* fall through */ }

        return DrawCmBitmap(size);
    }

    public static Bitmap DrawCmBitmap(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        int margin = Math.Max(1, size / 16);
        int radius = Math.Max(2, size / 5);
        var rect = new Rectangle(margin, margin, size - 2 * margin - 1, size - 2 * margin - 1);
        using var path = RoundedRect(rect, radius);
        using var brush = new SolidBrush(Color.FromArgb(64, 156, 255));
        g.FillPath(brush, path);
        float fontSize = size * 0.38f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.DrawString("CM", font, Brushes.White, new RectangleF(0, 0, size, size), sf);
        return bmp;
    }

    /// <summary>Draw rounded blue square (#409CFF) with white bold "CM".</summary>
    public static Icon CreateCmIcon(int size)
    {
        using var bmp = DrawCmBitmap(size);
        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            Native.NativeMethods.DestroyIcon(hIcon);
        }
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
