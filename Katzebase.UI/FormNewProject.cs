using Katzebase.UI.Classes;

namespace Katzebase.UI
{
    public partial class FormNewProject : Form
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDirectory { get; set; } = string.Empty;
        public string FullProjectFilePath => Path.Combine(ProjectDirectory, ProjectName) + Constants.PROJECT_EXTENSION;

        public FormNewProject()
        {
            InitializeComponent();
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {
            ProjectName = textBoxName.Text;
            ProjectDirectory = textBoxDirectory.Text;

            if (string.IsNullOrEmpty(ProjectName))
            {
                MessageBox.Show("You must specify a project name.", "Workload Generator", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            if (string.IsNullOrEmpty(ProjectDirectory))
            {
                MessageBox.Show("You must specify a project directory.", "Workload Generator", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            try
            {
                ProjectDirectory = Path.Combine(ProjectDirectory, ProjectName);
                Directory.CreateDirectory(ProjectDirectory);
                File.WriteAllText(Path.Combine(ProjectDirectory, $"{ProjectName}{Constants.PROJECT_EXTENSION}"), string.Empty);
            }
            catch
            {
                MessageBox.Show("Could not create project. Do you have access to the specified directory?.", "Workload Generator", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            Preferences.Instance.LastProjectDirectory = ProjectDirectory;
            Preferences.Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using (var browser = new FolderBrowserDialog())
            {
                if (string.IsNullOrEmpty(textBoxDirectory.Text) == false)
                {
                    browser.SelectedPath = textBoxDirectory.Text;
                }

                if (browser.ShowDialog() == DialogResult.OK)
                {
                    textBoxDirectory.Text = browser.SelectedPath;
                }
            }
        }

        private void FormNewProject_Load(object sender, EventArgs e)
        {
            textBoxDirectory.Text = Preferences.Instance.LastProjectDirectory;
        }
    }
}
