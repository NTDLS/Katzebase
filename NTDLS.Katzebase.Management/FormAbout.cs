using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace NTDLS.Katzebase.Management
{
    public partial class FormAbout : Form
    {
        readonly Assembly assembly = Assembly.GetExecutingAssembly();
        private Image? _originalImage;
        private double _rotationPhase = 0;
        private float _rotationAngle = 0;
        private System.Windows.Forms.Timer? _rotationTimer;

        public FormAbout()
        {
            InitializeComponent();
        }

        public FormAbout(bool showInTaskbar)
        {
            InitializeComponent();

            if (showInTaskbar)
            {
                ShowInTaskbar = true;
                StartPosition = FormStartPosition.CenterScreen;
                TopMost = true;
            }
            else
            {
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                TopMost = false;
            }
        }

        private void PictureBoxLogo_Paint(object? sender, PaintEventArgs e)
        {
            if (_originalImage == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            float cx = pictureBox1.Width / 2f;
            float cy = pictureBox1.Height / 2f;

            g.TranslateTransform(cx, cy);
            g.RotateTransform(_rotationAngle);
            g.TranslateTransform(-cx, -cy);

            // Compute the largest scale at which the image still fits inside the box
            // at the maximum rotation angle. For a rectangle (w,h) rotated by θ, its
            // axis-aligned bounding box is (w·cosθ + h·sinθ) × (w·sinθ + h·cosθ).
            const double maxAngleDeg = 8.0;
            double maxAngleRad = maxAngleDeg * Math.PI / 180.0;
            double cosA = Math.Cos(maxAngleRad);
            double sinA = Math.Sin(maxAngleRad);
            float imgW = _originalImage.Width;
            float imgH = _originalImage.Height;
            float boxW = pictureBox1.Width;
            float boxH = pictureBox1.Height;
            float scale = (float)Math.Min(
                boxW / (imgW * cosA + imgH * sinA),
                boxH / (imgW * sinA + imgH * cosA));
            float drawW = imgW * scale;
            float drawH = imgH * scale;
            float x = (boxW - drawW) / 2f;
            float y = (boxH - drawH) / 2f;

            g.DrawImage(_originalImage, x, y, drawW, drawH);
        }

        private void FormAbout_Load(object sender, EventArgs e)
        {
            AcceptButton = cmdOk;
            CancelButton = cmdOk;

            _originalImage = pictureBox1.Image;
            pictureBox1.Image = null;
            pictureBox1.Paint += PictureBoxLogo_Paint;

            _rotationTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _rotationTimer.Tick += (s, _) =>
            {
                _rotationPhase += 0.05;
                _rotationAngle = (float)(Math.Sin(_rotationPhase) * 8.0);
                pictureBox1.Invalidate();
            };
            _rotationTimer.Start();

            FormClosed += (s, _) => { _rotationTimer.Stop(); _rotationTimer.Dispose(); };

            if (assembly == null || assembly.Location == null)
            {
                return;
            }

            string? path = Path.GetDirectoryName(assembly.Location);
            if (path == null)
            {
                return;
            }

            path += @"\..\".Replace('/', '\\').Replace(@"\\", @"\");

            var files = Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories).ToList();
            files.AddRange(Directory.EnumerateFiles(path, "*.exe", SearchOption.AllDirectories));

            HashSet<string> seenAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                if (!seenAssemblies.Contains(fileName))
                {
                    seenAssemblies.Add(fileName);
                    AddAssembly(file);
                }
            }

            listViewVersions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewVersions.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void AddAssembly(string appPath)
        {
            try
            {
                var componentAssembly = AssemblyName.GetAssemblyName(appPath);
                var versionInfo = FileVersionInfo.GetVersionInfo(appPath);
                var companyName = versionInfo.CompanyName;

                if (componentAssembly.Version != null && companyName?.ToLower()?.Contains("networkdls") == true)
                {
                    var verStr = string.Join('.', componentAssembly.Version.ToString().Split('.').Take(3));
                    listViewVersions.Items.Add(new ListViewItem([componentAssembly.Name ?? "", verStr]));
                }
            }
            catch
            {
            }
        }

        private void LinkWebsiteKb_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://www.Katzebase.com",
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        private void LinkWebsiteNtdls_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://www.NetworkDLS.com",
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
