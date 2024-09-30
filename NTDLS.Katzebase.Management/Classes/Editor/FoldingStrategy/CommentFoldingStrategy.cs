using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace NTDLS.Katzebase.Management.Classes.Editor.FoldingStrategy
{
    public class CommentFoldingStrategy
    {
        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
        {
            var newFoldings = new List<NewFolding>();
            string text = document.Text;

            int commentStart = -1;

            for (int i = 0; i < text.Length - 1; i++)
            {
                // Detect the start of a block comment
                if (text[i] == '/' && text[i + 1] == '*')
                {
                    commentStart = i;
                }
                // Detect the end of a block comment
                else if (text[i] == '*' && text[i + 1] == '/' && commentStart != -1)
                {
                    newFoldings.Add(new NewFolding(commentStart, i + 2) { Name = "Comment" });
                    commentStart = -1;
                }
            }

            return newFoldings;
        }
    }
}
