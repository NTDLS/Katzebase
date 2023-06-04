using ICSharpCode.AvalonEdit;

namespace Katzebase.UI.Classes
{
    internal class TabFilePage : TabPage
    {
        public bool IsSaved { get; set; } = true;
        public TextEditor Editor { get; set; }
        public FormFindText FindTextForm { get; set; }
        public FormReplaceText ReplaceTextForm { get; set; }
        public string FilePath { get; set; } = string.Empty;

        public TabFilePage(string filePath, TextEditor editor) :
             base(filePath)
        {
            FilePath = filePath;
            Editor = editor;
            FindTextForm = new FormFindText(this);
            ReplaceTextForm = new FormReplaceText(this);
        }

        public bool Save()
        {
            IsSaved = true;
            return true;
        }
    }
}
