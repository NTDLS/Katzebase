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

            Activated += (object? sender, EventArgs e) => Opacity = 1.0;
            Deactivate += (object? sender, EventArgs e) =>
            {
                if (!Disposing)
                {
                    Opacity = 0.75;
                }
            };

            tabControlBody.SelectedIndexChanged += TabControlBody_SelectedIndexChanged;

            Shown += (object? sender, EventArgs e) =>
            {
                if (findType == FindType.Find)
                {
                    AcceptButton = buttonFind_FindNext;
                    tabControlBody.SelectedTab = tabPageFind;
                    textBoxFindText.Focus();
                }
                if (findType == FindType.Replace)
                {
                    AcceptButton = buttonReplace_FindNext;
                    tabControlBody.SelectedTab = tabPageReplace;
                    textBoxFindReplaceText.Focus();
                }
            };

            CancelButton = buttonFind_Close;
        }
        private void FormFind_Load(object sender, EventArgs e)
        {
            if (Owner != null)
            {
                var currentTab = _studioForm?.CurrentTabFilePage();
                if (currentTab != null)
                {
                    var absolutePoint = currentTab.TabControlParent.Parent?.PointToScreen(currentTab.TabControlParent.Location);
                    if (absolutePoint != null && absolutePoint.HasValue)
                    {
                        //Place the find form in a reasonable location.
                        Location = new Point(
                            (absolutePoint.Value.X + currentTab.Width) - (Width + 50),
                            absolutePoint.Value.Y + 50);
                    }
                }
            }
        }

        private void TabControlBody_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (tabControlBody.SelectedTab == tabPageFind)
            {
                AcceptButton = buttonFind_FindNext;
                textBoxFindText.Focus();
            }
            else if (tabControlBody.SelectedTab == tabPageReplace)
            {
                AcceptButton = buttonReplace_FindNext;
                textBoxFindReplaceText.Focus();
            }
        }

        private void FormFind_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void ButtonFindNext_Click(object sender, EventArgs e)
        {
            _studioForm?.FindNext(textBoxFindText.Text);
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void buttonReplace_Close_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void buttonReplace_FindNext_Click(object sender, EventArgs e)
        {

        }

        private void buttonReplace_Replace_Click(object sender, EventArgs e)
        {

        }

        private void buttonReplace_ReplaceAll_Click(object sender, EventArgs e)
        {

        }
    }
}
