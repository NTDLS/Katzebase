namespace NTDLS.Katzebase.Management
{
    public partial class FormFindText : Form
    {
        private readonly FormStudio? _studioForm;

        public enum FindType
        {
            Find,
            Replace
        }

        public FormFindText()
        {
            InitializeComponent();
        }

        public FormFindText(FormStudio studioForm, FindType findType, string searchText, string replaceText)
        {
            InitializeComponent();
            _studioForm = studioForm;
            textBoxFindText.Text = searchText;
            textBoxFindReplaceText.Text = replaceText;
            Owner = studioForm;

            StartPosition = FormStartPosition.CenterParent;
            Deactivate += FormFindText_Deactivate;
            Activated += FormFindText_Activated;
            Shown += FormFindText_Shown;

            tabControlBody.TabIndexChanged += TabControlBody_TabIndexChanged;

            if (findType == FindType.Find)
            {
                tabControlBody.SelectedTab = tabPageFind;
            }
            if (findType == FindType.Replace)
            {
                tabControlBody.SelectedTab = tabPageReplace;
            }
        }

        private void TabControlBody_TabIndexChanged(object? sender, EventArgs e)
        {
            if (tabControlBody.SelectedTab == tabPageFind)
            {
                AcceptButton = buttonFind_FindNext;
            }
            else if (tabControlBody.SelectedTab == tabPageReplace)
            {
                AcceptButton = buttonReplace_FindNext;
            }
        }

        private void FormFindText_Shown(object? sender, EventArgs e)
        {
            if (Owner != null)
            {
                int x = Owner.Location.X + (Owner.Width - this.Width) / 2;
                int y = Owner.Location.Y + (Owner.Height - this.Height) / 2;
                Location = new Point(x, y);
            }
        }

        private void FormFindText_Activated(object? sender, EventArgs e)
        {
            Opacity = 1.0;
        }

        private void FormFindText_Deactivate(object? sender, EventArgs e)
        {
            if (!Disposing)
            {
                Opacity = 0.75;
            }
        }

        private void FormFind_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void FormFind_Load(object sender, EventArgs e)
        {
            AcceptButton = buttonFind_FindNext;
            CancelButton = buttonFind_Close;
        }

        private void ButtonFindNext_Click(object sender, EventArgs e)
        {
            _studioForm?.FindNext(textBoxFindText.Text);
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
