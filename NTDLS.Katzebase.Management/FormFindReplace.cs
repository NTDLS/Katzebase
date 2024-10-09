namespace NTDLS.Katzebase.Management
{
    public partial class FormFindReplace : Form
    {
        private readonly FormStudio? _studioForm;
        private bool _isFirstLoad = true;

        public enum FindType
        {
            Find,
            Replace
        }

        public FormFindReplace()
        {
            InitializeComponent();
        }

        public FormFindReplace(FormStudio studioForm, string searchText, string replaceText)
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
        }

        public void Show(FindType findType)
        {
            Show();

            if (findType == FindType.Find)
            {
                AcceptButton = buttonFind_FindNext;
                CancelButton = buttonFind_Close;
                tabControlBody.SelectedTab = tabPageFind;
                textBoxFindText.Focus();
                Text = "Find";
            }
            if (findType == FindType.Replace)
            {
                AcceptButton = buttonReplace_FindNext;
                CancelButton = buttonReplace_Close;
                tabControlBody.SelectedTab = tabPageReplace;
                textBoxFindReplaceText.Focus();
                Text = "Replace";
            }
        }

        private void FormFind_Load(object sender, EventArgs e)
        {
            if (Owner != null && _isFirstLoad)
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
            _isFirstLoad = false;
        }

        private void TabControlBody_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (tabControlBody.SelectedTab == tabPageFind)
            {
                AcceptButton = buttonFind_FindNext;
                CancelButton = buttonFind_Close;
                textBoxFindText.Focus();
                Text = "Find";
            }
            else if (tabControlBody.SelectedTab == tabPageReplace)
            {
                AcceptButton = buttonReplace_FindNext;
                CancelButton = buttonReplace_Close;
                textBoxFindReplaceText.Focus();
                Text = "Replace";
            }
        }

        private void FormFind_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void ButtonFindNext_Click(object sender, EventArgs e)
            => _studioForm?.FindNext(textBoxFindText.Text, checkBoxFindCaseSensitive.Checked);

        private void ButtonReplace_FindNext_Click(object sender, EventArgs e)
            => _studioForm?.FindNext(textBoxFindReplaceText.Text, checkBoxFindReplaceCaseSensitive.Checked);

        private void ButtonReplace_Replace_Click(object sender, EventArgs e)
            => _studioForm?.FindReplace(textBoxFindReplaceText.Text, textBoxFindReplaceWithText.Text, checkBoxFindReplaceCaseSensitive.Checked);

        private void ButtonReplace_ReplaceAll_Click(object sender, EventArgs e)
            => _studioForm?.FindReplaceAll(textBoxFindReplaceText.Text, textBoxFindReplaceWithText.Text, checkBoxFindReplaceCaseSensitive.Checked);

        private void ButtonClose_Click(object sender, EventArgs e)
            => Hide();
        private void ButtonReplace_Close_Click(object sender, EventArgs e)
            => Hide();
    }
}
