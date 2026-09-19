using ClipMemo.Forms;

namespace ClipMemo;

internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    private static void Main()
    {
        _mutex = new Mutex(true, @"Local\ClipMemo_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "ClipMemo sudah berjalan (lihat system tray).",
                "ClipMemo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());

        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
