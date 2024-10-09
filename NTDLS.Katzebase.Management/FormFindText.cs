using static NTDLS.Katzebase.Management.FormFindText;

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

            tabControlBody.TabIndexChanged += TabControlBody_TabIndexChanged;

            if (findType == FindType.Find)
            {
                AcceptButton = buttonFind_FindNext;
                tabControlBody.SelectedTab = tabPageFind;
            }
            if (findType == FindType.Replace)
            {
                AcceptButton = buttonReplace_FindNext;
                tabControlBody.SelectedTab = tabPageReplace;
            }

            CancelButton = buttonFind_Close;
        }

        private void FormFind_Load(object sender, EventArgs e)
        {
            StartPosition = FormStartPosition.CenterParent;

            /*
            if (Owner != null)
            {
                int x = Owner.Location.X + (Owner.Width - this.Width) / 2;
                int y = Owner.Location.Y + (Owner.Height - this.Height) / 2;
                Location = new Point(x, y);
            }
            */
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
    }
}
