using ICSharpCode.AvalonEdit;
using Katzebase.PublicLibrary.Client;

namespace Katzebase.UI.Classes
{
    internal class TabFilePage : TabPage
    {
        public bool IsSaved { get; set; } = true;
        public TextEditor Editor { get; private set; }
        public FormFindText FindTextForm { get; private set; }
        public FormReplaceText ReplaceTextForm { get; private set; }
        public string ServerAddressURL { get; set; }
        public KatzebaseClient Client { get; private set; }


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

        public TabFilePage(string serverAddressURL, string filePath, TextEditor editor) :
             base(filePath)
        {
            FilePath = filePath;
            Editor = editor;
            FindTextForm = new FormFindText(this);
            ReplaceTextForm = new FormReplaceText(this);
            ServerAddressURL = serverAddressURL;
            Client = new KatzebaseClient(ServerAddressURL);
        }

        public void Save()
        {
            File.WriteAllText(FilePath, Editor.Text);
            IsSaved = true;
        }
    }
}
