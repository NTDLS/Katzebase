using Katzebase.UI.Classes;

namespace Katzebase.UI
{
    public partial class FormWelcome : Form
    {
        public enum Result
        {
            Cancel,
            OpenExisting,
            OpenRecent,
            CreateNew,
        }

        public Result FormalResult { get; set; }
        public string SelectedProject => listBoxRecent.SelectedItem?.ToString() ?? string.Empty;

        public FormWelcome()
        {
            InitializeComponent();
        }

        private void FormWelcome_Load(object sender, EventArgs e)
        {
            this.AcceptButton = radButtonOk;
            this.CancelButton = radButtonCancel;

            listBoxRecent.MouseUp += ListBoxRecent_MouseUp;
            listBoxRecent.MouseDoubleClick += ListBoxRecent_MouseDoubleClick;

            try
            {
                foreach (var recentProject in Preferences.Instance.RecentProjects)
                {
                    listBoxRecent.Items.Add(recentProject);
                }
            }
            catch
            {
            }

            radioButtonOpenRecent.Enabled = listBoxRecent.Items.Count > 0;
            listBoxRecent.Enabled = (listBoxRecent.Items.Count > 0 && radioButtonOpenRecent.Checked == true);
        }

        private void ListBoxRecent_MouseUp(object? sender, MouseEventArgs e)
        {
            var clickedItemIndex = listBoxRecent.IndexFromPoint(e.Location);
            if (clickedItemIndex < 0)
            {
                return;
            }

            var clickedItem = listBoxRecent.Items[clickedItemIndex];
            if (clickedItem == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                ContextMenuStrip recentProjectMenu = new ContextMenuStrip();
                recentProjectMenu.ItemClicked += RecentProjectMenu_ItemClicked;

                if (clickedItem != null)
                {
                    listBoxRecent.SelectedItem = clickedItem;
                    recentProjectMenu.Items.Add("Remove");
                    recentProjectMenu.Items.Add("Clear All");
                    recentProjectMenu.Tag = listBoxRecent.SelectedItem;
                }
                else
                {
                    recentProjectMenu.Items.Add("Clear All");
                }

                recentProjectMenu.Show(Cursor.Position);
                recentProjectMenu.Visible = true;
            }
        }

        private void RecentProjectMenu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            var contextMenu = sender as ContextMenuStrip;
            if (contextMenu == null)
            {
                return;
            }

            string? clickedItem = contextMenu.Tag as string;
            if (clickedItem == null)
            {
                return;
            }

            if (e?.ClickedItem?.Text == "Remove")
            {
                Preferences.Instance.RemoveRecentProject(clickedItem);
                listBoxRecent.Items.Remove(clickedItem);
            }
            else if (e?.ClickedItem?.Text == "Clear All")
            {
                Preferences.Instance.RecentProjects.Clear();
                listBoxRecent.Items.Clear();
            }

            Preferences.Save();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            Preferences.Instance.ShowWelcome = !checkBoxDontShowAgain.Checked;

            if (radioButtonOpenRecent.Checked)
            {
                if (string.IsNullOrEmpty(SelectedProject))
                {
                    MessageBox.Show("Select a file from the list or choose another option.",
                        "Select a Recent Project", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }

                FormalResult = Result.OpenRecent;
            }
            else if (radioButtonOpenExisting.Checked)
            {
                FormalResult = Result.OpenExisting;
            }
            else if (radioButtonCreateNew.Checked)
            {
                FormalResult = Result.CreateNew;
            }

            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Preferences.Instance.ShowWelcome = !checkBoxDontShowAgain.Checked;
            FormalResult = Result.Cancel;
            this.Close();
        }

        private void ListBoxRecent_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            var clickedItemIndex = listBoxRecent.IndexFromPoint(e.Location);
            if (clickedItemIndex < 0)
            {
                return;
            }

            var clickedItem = listBoxRecent.Items[clickedItemIndex];
            if (clickedItem == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (File.Exists(SelectedProject) == false && string.IsNullOrWhiteSpace(SelectedProject) == false)
                {
                    MessageBox.Show("The selected project no longer exists.", "Missing Project.");
                    Preferences.Instance.RemoveRecentProject(SelectedProject);
                    listBoxRecent.Items.Remove(clickedItem);
                    return;
                }

                FormalResult = Result.OpenRecent;
                DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void radioButtonOpenRecent_CheckedChanged(object sender, EventArgs e)
        {
            listBoxRecent.Enabled = (listBoxRecent.Items.Count > 0 && radioButtonOpenRecent.Checked == true);
        }
    }
}