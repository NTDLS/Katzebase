using ICSharpCode.AvalonEdit;
using Katzebase.Library.Client;

namespace Katzebase.UI.Classes
{
    internal class TabFilePage : TabPage
    {
        public bool IsSaved { get; set; } = true;
        public TextEditor Editor { get; set; }
        public FormFindText FindTextForm { get; set; }
        public FormReplaceText ReplaceTextForm { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string ServerAddressURL { get; set; }
        public KatzebaseClient Client { get; set; }

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

        public bool Save()
        {
            IsSaved = true;
            return true;
        }
    }
}
