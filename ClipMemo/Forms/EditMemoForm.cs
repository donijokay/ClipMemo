namespace ClipMemo.Forms;

public sealed class EditMemoForm : Form
{
    private readonly TextBox _box;
    public string EditedText => _box.Text;

    public EditMemoForm(string initial)
    {
        Text = "Edit Memo";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Size = new Size(420, 280);
        MinimumSize = new Size(320, 200);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9f);

        var label = new Label
        {
            Text = "Edit teks:",
            AutoSize = true,
            Location = new Point(12, 12),
            ForeColor = Color.WhiteSmoke,
        };

        _box = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            Location = new Point(12, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Size = new Size(ClientSize.Width - 24, ClientSize.Height - 90),
            Text = initial,
            BackColor = Color.FromArgb(45, 45, 50),
            ForeColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
        };

        var btnSave = new Button
        {
            Text = "Simpan",
            DialogResult = DialogResult.OK,
            Size = new Size(88, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(64, 156, 255),
            ForeColor = Color.White,
        };
        btnSave.Location = new Point(ClientSize.Width - 12 - 88 - 8 - 88, ClientSize.Height - 42);

        var btnCancel = new Button
        {
            Text = "Batal",
            DialogResult = DialogResult.Cancel,
            Size = new Size(88, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 66),
            ForeColor = Color.WhiteSmoke,
        };
        btnCancel.Location = new Point(ClientSize.Width - 12 - 88, ClientSize.Height - 42);

        Controls.Add(label);
        Controls.Add(_box);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        Shown += (_, _) =>
        {
            _box.SelectionStart = _box.TextLength;
            _box.Focus();
        };
    }
}
