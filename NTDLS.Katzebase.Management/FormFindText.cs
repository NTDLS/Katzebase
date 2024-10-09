namespace NTDLS.Katzebase.Management
{
    public partial class FormFindText : Form
    {
        private readonly FormStudio? _studioForm;

        public FormFindText()
        {
            InitializeComponent();
        }

        public FormFindText(FormStudio studioForm, string searchText)
        {
            InitializeComponent();
            _studioForm = studioForm;
            textBoxFindText.Text = searchText;
            Owner = studioForm;

            StartPosition = FormStartPosition.CenterParent;
            Deactivate += FormFindText_Deactivate;
            Activated += FormFindText_Activated;
            Shown += FormFindText_Shown;
        }

        private void FormFindText_Shown(object? sender, EventArgs e)
        {
            if (Owner != null)
            {
                int x = Owner.Location.X + (Owner.Width - this.Width) / 2;
                int y = Owner.Location.Y + (Owner.Height - this.Height) / 2;
                Location = new System.Drawing.Point(x, y);
            }
        }

        private void FormFindText_Activated(object? sender, EventArgs e)
        {
            Opacity = 1.0;
        }

        private void FormFindText_Deactivate(object? sender, EventArgs e)
        {
            Opacity = 0.75;
        }

        private void FormFind_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void FormFind_Load(object sender, EventArgs e)
        {
            AcceptButton = buttonFindNext;
            CancelButton = buttonClose;
        }

        private void ButtonFind_Click(object sender, EventArgs e)
        {
            _studioForm?.FindFirst(textBoxFindText.Text);
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
