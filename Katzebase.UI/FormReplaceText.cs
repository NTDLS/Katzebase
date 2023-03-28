using Katzebase.UI.Classes;

namespace Katzebase.UI
{
    internal partial class FormReplaceText : Form
    {
        private ProjectTabPage? _projectTabPage;
        private int _lastIndex = -1;

        public FormReplaceText()
        {
            InitializeComponent();
        }

        public FormReplaceText(ProjectTabPage projectTabPage)
        {
            InitializeComponent();
            _projectTabPage = projectTabPage;

            textBoxFindText.TextChanged += TextBoxFindText_TextChanged;
            this.FormClosing += FormReplace_FormClosing;
        }

        private void FormReplace_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void TextBoxFindText_TextChanged(object? sender, EventArgs e)
        {
            _lastIndex = -1;
        }

        private void FormFind_Load(object sender, EventArgs e)
        {
            AcceptButton = buttonReplace;
            CancelButton = buttonClose;
        }

        private bool DoFind(int startIndex)
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
                    return true;
                }
            }
            return false;
        }

        public void FindFirst()
        {
            DoFind(0);
        }

        public void FindNext()
        {
            DoFind(_lastIndex);
        }

        public void Replace()
        {
            if (_projectTabPage?.Editor?.SelectionLength <= 0)
            {
                DoFind(_lastIndex);
            }

            if (_projectTabPage?.Editor?.SelectionLength > 0)
            {
                _projectTabPage.Editor.SelectedText = textBoxReplaceWith.Text;
                DoFind(_lastIndex);
            }
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
            this.Hide();
        }

        private void buttonReplace_Click(object sender, EventArgs e)
        {
            Replace();
        }

        private void buttonReplaceAll_Click(object sender, EventArgs e)
        {
            ReplaceAll();
        }

        public void ReplaceAll()
        {
            if (_projectTabPage != null)
            {
                do
                {
                    if (DoFind(_lastIndex))
                    {
                        _projectTabPage.Editor.SelectedText = textBoxReplaceWith.Text;
                    }
                } while (_lastIndex > 0);
            }
        }
    }
}