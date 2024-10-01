using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Text;

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
                    var labelBuilder = new StringBuilder();

                    int segmentLength = i - commentStart;
                    int labelParseIndex = 2; //Skip "/*"

                    //Skip leading whitespace.
                    for (; labelParseIndex < 100 && commentStart + labelParseIndex < i; labelParseIndex++)
                    {
                        if (char.IsWhiteSpace(text[commentStart + labelParseIndex]) == false)
                        {
                            break;
                        }
                    }

                    //Get lable up to first newline or 100 charcters, which ever comes first.
                    for (int labelLength = 0; labelLength < 100 && commentStart + labelLength < i; labelLength++, labelParseIndex++)
                    {
                        if (text[commentStart + labelParseIndex] == '\r' || text[commentStart + labelParseIndex] == '\n')
                        {
                            break;
                        }

                        labelBuilder.Append(text[commentStart + labelParseIndex]);
                    }

                    string label = labelBuilder.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        label = "Comment.";
                    }

                    newFoldings.Add(new NewFolding(commentStart, i + 2) { Name = label });
                    commentStart = -1;
                }
            }

            return newFoldings;
        }
    }
}
