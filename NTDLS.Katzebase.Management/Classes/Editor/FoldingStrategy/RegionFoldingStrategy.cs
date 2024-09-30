using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace NTDLS.Katzebase.Management.Classes.Editor.FoldingStrategy
{
    public class RegionFoldingStrategy
    {
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var newFoldings = CreateNewFoldings(document);
            manager.UpdateFoldings(newFoldings, -1);
        }

        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
        {
            var newFoldings = new List<NewFolding>();
            var startOffsets = new Stack<int>();
            var startLines = new Stack<string>();  // Stack to hold region names
            string documentText = document.Text;

            for (int lineNumber = 1; lineNumber <= document.LineCount; lineNumber++)
            {
                DocumentLine line = document.GetLineByNumber(lineNumber);
                string lineText = document.GetText(line.Offset, line.Length).Trim();

                if (lineText.StartsWith("--region", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the region name (text after #region)
                    string regionName = lineText.Length > 8 ? lineText.Substring(8).Trim() : "Unnamed Region";

                    // Record the starting offset and the region name
                    startOffsets.Push(line.Offset);
                    startLines.Push(regionName);
                }
                else if (lineText.StartsWith("--endregion", StringComparison.OrdinalIgnoreCase))
                {
                    if (startOffsets.Count > 0)
                    {
                        // Found a matching #endregion for the most recent #region
                        int startOffset = startOffsets.Pop();
                        string regionName = startLines.Pop();  // Get the region name
                        int endOffset = line.EndOffset;

                        // Create a new folding from #region to #endregion, with the region name as the fold title
                        newFoldings.Add(new NewFolding(startOffset, endOffset)
                        {
                            Name = regionName
                        });
                    }
                }
            }

            // Sort the foldings by their start offset
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

            return newFoldings;
        }
    }
}
