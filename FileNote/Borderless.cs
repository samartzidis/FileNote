using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Trinet.Core.IO.Ntfs;

namespace FileNote
{
    public partial class Borderless : Form
    {
        private bool _dragging;
        private Point _offset;
        private readonly string _path;
        private readonly Color _backColor = Color.FromArgb(0xFF, 0xFF, 0x99); // Canary #FFFF99
        private const int GripSize = 16;
        private const int CaptionBarHeight = 32;

        public Borderless(string path)
        {
            _path = path;

            InitializeComponent();
        }

        private void Borderless_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            StartPosition = FormStartPosition.Manual;
            Location = Cursor.Position;

            BackColor = _backColor;
            textBox.BackColor = _backColor;

            MinimumSize = Size;

            labelTitle.MouseDown += Borderless_MouseDown;
            labelTitle.MouseUp += Borderless_MouseUp;
            labelTitle.MouseMove += Borderless_MouseMove;

            var fileTitle = _path;
            if (fileTitle != null && fileTitle.Length > 32)
                fileTitle = "..." + fileTitle.Substring(fileTitle.Length - 32);

            labelTitle.Text = $"FileNote - {fileTitle}";
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            var ads = GetCommentsStream();

            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                ads.Delete();
            }
            else
            {
                using (var fs = ads.OpenWrite())
                using (var sr = new StreamWriter(fs))
                    sr.Write(textBox.Text);
            }

            Close();
        }

        private void Borderless_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                var currentScreenPos = PointToScreen(e.Location);
                Location = new Point(currentScreenPos.X - _offset.X, currentScreenPos.Y - _offset.Y);
            }
        }

        private void Borderless_MouseDown(object sender, MouseEventArgs e)
        {
            _offset.X = e.X;
            _offset.Y = e.Y;
            _dragging = true;
        }

        private void Borderless_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var rc = new Rectangle(ClientSize.Width - GripSize, ClientSize.Height - GripSize, GripSize, GripSize);
            ControlPaint.DrawSizeGrip(e.Graphics, BackColor, rc);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84) // WM_NCHITTEST
            { 
                Point pos = new Point(m.LParam.ToInt32());
                pos = PointToClient(pos);
                if (pos.Y < CaptionBarHeight)
                {
                    m.Result = (IntPtr)2;  // HTCAPTION
                    return;
                }
                if (pos.X >= ClientSize.Width - GripSize && pos.Y >= ClientSize.Height - GripSize)
                {
                    m.Result = (IntPtr)17; // HTBOTTOMRIGHT
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private void Borderless_Shown(object sender, EventArgs e)
        {
            var ads = GetCommentsStream();
            if (!ads.Exists)
                return;

            using (var stream = ads.OpenText())
            {
                textBox.Text = stream.ReadToEnd();
            }
        }

        private AlternateDataStreamInfo GetCommentsStream()
        {
            FileSystemInfo fi = null;
            if (Directory.Exists(_path))
                fi = new DirectoryInfo(_path);
            else if (File.Exists(_path))
                fi = new FileInfo(_path);

            return fi.GetAlternateDataStream("FileNote");
        }
    }
}
