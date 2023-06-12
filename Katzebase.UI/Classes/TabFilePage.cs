using ICSharpCode.AvalonEdit;
using Katzebase.PublicLibrary.Client;

namespace Katzebase.UI.Classes
{
    internal class TabFilePage : TabPage
    {
        private bool _isSaved = false;

        public bool IsSaved
        {
            get => _isSaved;

            set
            {
                _isSaved = value;
                if (_isSaved == true)
                {
                    this.Text = Text.TrimEnd('*');
                }
            }
        }

        public TextEditor Editor { get; private set; }
        public FormFindText FindTextForm { get; private set; }
        public FormReplaceText ReplaceTextForm { get; private set; }
        public string ServerAddressURL { get; set; }
        public KatzebaseClient? Client { get; private set; }
        public bool IsFileOpen { get; private set; } = false;

        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set
            {
                this.Text = Path.GetFileName(value);
                _filePath = value;
            }
        }

        public TabFilePage(string serverAddressURL, string tabText, TextEditor editor) :
             base(tabText)
        {
            Editor = editor;
            FindTextForm = new FormFindText(this);
            ReplaceTextForm = new FormReplaceText(this);
            ServerAddressURL = serverAddressURL;
            if (string.IsNullOrEmpty(serverAddressURL) == false)
            {
                Client = new KatzebaseClient(ServerAddressURL);
            }
        }

        public void OpenFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                Editor.Document.FileName = filePath;
                Editor.Text = File.ReadAllText(Editor.Document.FileName);
                IsSaved = true;
            }

            this.FilePath = filePath;

            IsFileOpen = true;
        }

        public bool Save(string fileName)
        {
            File.WriteAllText(fileName, Editor.Text);
            IsSaved = true;
            this.OpenFile(fileName);
            return true;
        }

        public bool Save()
        {
            if (IsFileOpen)
            {
                File.WriteAllText(FilePath, Editor.Text);
                IsSaved = true;
                return true;
            }
            return false;
        }
    }
}
