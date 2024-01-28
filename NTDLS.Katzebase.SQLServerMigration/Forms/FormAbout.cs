using System.Diagnostics;
using System.Reflection;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public partial class FormAbout : Form
    {
        readonly Assembly assembly = Assembly.GetExecutingAssembly();

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

        private void FormAbout_Load(object sender, EventArgs e)
        {
            AcceptButton = cmdOk;
            CancelButton = cmdOk;

            if (assembly == null || assembly.Location == null)
            {
                return;
            }

            string? path = Path.GetDirectoryName(assembly.Location);
            if (path == null)
            {
                return;
            }

            var files = Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly).ToList();
            files.AddRange(Directory.EnumerateFiles(path, "*.exe", SearchOption.TopDirectoryOnly).ToList());

            foreach (var file in files)
            {
                AddApplication(file);
            }

            listViewVersions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewVersions.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void AddApplication(string appPath)
        {
            try
            {
                var componentAssembly = AssemblyName.GetAssemblyName(appPath);
                var versionInfo = FileVersionInfo.GetVersionInfo(appPath);
                var companyName = versionInfo.CompanyName;

                if (componentAssembly.Version != null && companyName?.ToLower()?.Contains("networkdls") == true)
                {
                    listViewVersions.Items.Add(new ListViewItem(new string[] { componentAssembly.Name ?? "", componentAssembly.Version.ToString() }));
                }
            }
            catch
            {
            }
        }

        private void linkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.NetworkDLS.com");
        }
    }
}
