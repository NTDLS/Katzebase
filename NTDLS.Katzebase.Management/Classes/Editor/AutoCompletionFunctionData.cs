using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Windows;
using System.Windows.Controls;

namespace NTDLS.Katzebase.Management.Classes.Editor
{
    /// <summary>
    /// Completion data for function code completion window.
    /// </summary>
    internal class AutoCompletionFunctionData : ICompletionData
    {
        private readonly AutoCompleteFunction _function;

        public AutoCompletionFunctionData(AutoCompleteFunction function)
        {
            _function = function;
            Text = _function.Name;
        }

        public string Text { get; private set; }

        /// <summary>
        /// This is what will appear in the completion list
        /// </summary>
        public object Content
        {
            get { return $"{_function.Name}"; }
        }

        /// <summary>
        /// Description when the item is highlighted
        /// </summary>
        public object Description
        {
            get
            {
                var tooltipText = new TextBlock
                {
                    Text = $"{_function.Description}",
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300
                };

                if (string.IsNullOrEmpty(_function.ReturnType) == false)
                {
                    tooltipText.Text += $"Returns: {_function.ReturnType}";
                }

                return tooltipText;
            }
        }

        public System.Windows.Media.ImageSource? Image => null;

        public double Priority => 0;

        /// <summary>
        /// When the function is selected, insert the function name and parameters
        /// </summary>
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var functionSignature = $"{_function.Name}({GetParametersString()})";
            textArea.Document.Replace(completionSegment, functionSignature);
            textArea.Caret.Offset -= 1; // Move caret just inside the last parenthesis for user to fill in params
        }

        /// <summary>
        /// Helper method to get parameters formatted for the function signature
        /// </summary>
        private string GetParametersString()
        {
            return string.Join(", ", _function.Parameters.Select(p => $"{p.DataType} {p.Name}"));
        }
    }
}
