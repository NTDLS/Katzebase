using NTDLS.Katzebase.Client.UI.Controls;

namespace NTDLS.Katzebase.Client.UI
{
    internal partial class FormFindText : Form
    {
        private readonly TabFilePage? _projectTabPage;
        private int _lastIndex = -1;
        public string SearchText => textBoxFindText.Text;

        public FormFindText()
        {
            InitializeComponent();
            textBoxFindText.TextChanged += TextBoxFindText_TextChanged;
        }

        public FormFindText(TabFilePage projectTabPage)
        {
            InitializeComponent();
            _projectTabPage = projectTabPage;
        }

        private void FormFind_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void TextBoxFindText_TextChanged(object? sender, EventArgs e)
        {
            _lastIndex = -1;
        }

        private void FormFind_Load(object sender, EventArgs e)
        {
            AcceptButton = buttonFindNext;
            CancelButton = buttonClose;
        }

        private void DoFind(int startIndex)
        {
            if (_projectTabPage?.Editor != null)
            {
                string findText = textBoxFindText.Text;
                _lastIndex = _projectTabPage.Editor.Document.IndexOf(findText, (startIndex + 1),
                    (_projectTabPage.Editor.Document.TextLength - startIndex) - 1, StringComparison.CurrentCultureIgnoreCase);
                if (_lastIndex >= 0)
                {
                    _projectTabPage.Editor.Select(_lastIndex, findText.Length);
                    _projectTabPage.Editor.TextArea.Caret.BringCaretToView();
                }
            }
        }

        public void FindFirst()
        {
            DoFind(0);
        }

        public void FindNext()
        {
            DoFind(_lastIndex);
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            FindFirst();
        }

        private void buttonFindNext_Click(object sender, EventArgs e)
        {
            FindNext();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
