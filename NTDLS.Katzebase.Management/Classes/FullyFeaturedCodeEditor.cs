using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NTDLS.Helpers;
using NTDLS.Katzebase.Management.Classes.Editor;
using NTDLS.Katzebase.Management.Classes.Editor.FoldingStrategy;
using NTDLS.Katzebase.Management.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;

namespace NTDLS.Katzebase.Management.Classes
{
    internal class FullyFeaturedCodeEditor : TextEditor
    {
        public CodeTabPage CodeTabPage { get; private set; }
        private System.Windows.Forms.Integration.ElementHost? _controlHost;
        private readonly ContextMenuStrip _contextMenu;
        private readonly FoldingManager _foldingManager;
        private readonly RegionFoldingStrategy _regionFoldingStrategy;
        private readonly CommentFoldingStrategy _commentFoldingStrategy;
        private readonly DispatcherTimer _foldingUpdateTimer;
        private CompletionWindow? _completionWindow;

        public FullyFeaturedCodeEditor(CodeTabPage codeTabPage)
        {
            CodeTabPage = codeTabPage;

            using var stringReader = new StringReader(Properties.Resources.Highlighter);
            using var reader = XmlReader.Create(stringReader);
            SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            reader.Close();
            stringReader.Close();

            ApplyEditorSettings(this);

            _foldingManager = FoldingManager.Install(TextArea);
            _regionFoldingStrategy = new RegionFoldingStrategy();
            _commentFoldingStrategy = new CommentFoldingStrategy();

            // Set up a timer for batch updates (500ms)
            _foldingUpdateTimer = new DispatcherTimer();
            _foldingUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
            _foldingUpdateTimer.Tick += (object? sender, EventArgs e) =>
            {
                _foldingUpdateTimer.Stop();

                var newFolds = new List<NewFolding>();

                newFolds.AddRange(_regionFoldingStrategy.CreateNewFoldings(Document));
                newFolds.AddRange(_commentFoldingStrategy.CreateNewFoldings(Document));

                _foldingManager.UpdateFoldings(newFolds, -1);
            };

            TextChanged += (sender, e) => // Hook into the TextChanged event to restart the timer
            {
                _foldingUpdateTimer.Stop();
                _foldingUpdateTimer.Start();
            };

            // Hook into the key down event to trigger completion
            TextArea.TextEntering += TextArea_TextEntering;
            TextArea.TextEntered += TextArea_TextEntered;

            DragEnter += Editor_DragEnter;
            Drop += Editor_Drop;
            KeyUp += Editor_KeyUp;

            _contextMenu = new ContextMenuStrip();

            MouseUp += FullyFeaturedCodeEditor_MouseUp;
        }

        #region Code Completion.

        enum CompletionType
        {
            SystemFunction,
            Schema
        }

        // Triggered when the user enters a character (e.g., '.')
        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == " ")
            {
                var currentOffset = CaretOffset;
                string textBeforeCaret = Document.GetText(0, currentOffset);

                // Check if the last word entered is "exec "
                if (textBeforeCaret.EndsWith("exec ", StringComparison.InvariantCultureIgnoreCase))
                {
                    ShowCompletionWindow(CompletionType.SystemFunction);
                }
            }
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (_completionWindow != null)
            {
                // If the completion window is open, and the user types something that should close it
                if (e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
                {
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }

        /// <summary>
        /// Show code completion window with a list of options
        /// </summary>
        private void ShowCompletionWindow(CompletionType completionType)
        {
            _completionWindow = new CompletionWindow(TextArea);
            var data = _completionWindow.CompletionList.CompletionData;

            if (completionType == CompletionType.SystemFunction)
            {
                var systemFunctions = GlobalState.AutoCompleteFunctions.Where(o => o.Type == AutoCompleteFunction.FunctionType.System);
                foreach (var systemFunction in systemFunctions)
                {
                    data.Add(new AutoCompletionFunctionData(systemFunction));
                }
            }

            _completionWindow.Show();

            _completionWindow.Closed += delegate
            {
                _completionWindow = null;
            };
        }

        #endregion

        public void AssociateContainer(System.Windows.Forms.Integration.ElementHost controlHost)
        {
            _controlHost = controlHost;
        }

        public static void ApplyEditorSettings(TextEditor editor)
        {
            editor.ShowLineNumbers = Program.Settings.EditorShowLineNumbers;
            editor.FontFamily = new System.Windows.Media.FontFamily(Program.Settings.EditorFontFamily);
            editor.FontSize = Program.Settings.EditorFontSize;
            editor.WordWrap = Program.Settings.EditorWordWrap;
        }

        private void CollapseAllFolds()
        {
            foreach (var folding in _foldingManager.AllFoldings)
            {
                folding.IsFolded = true;
            }
        }

        private void ExpandAllFolds()
        {
            foreach (var folding in _foldingManager.AllFoldings)
            {
                folding.IsFolded = false;
            }
        }

        private void FullyFeaturedCodeEditor_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Right)
            {
                return;
            }

            try
            {
                _contextMenu.Items.Clear();

                var clickPosition = GetPositionFromPoint(e.GetPosition(this));
                if (clickPosition != null)
                {
                    TextArea.Caret.Position = clickPosition.Value;
                }

                _contextMenu.Items.Add(new ToolStripMenuItem("Cut", null, (object? sender, EventArgs e) => Cut())
                {
                    Enabled = !string.IsNullOrEmpty(this.SelectedText)
                });

                _contextMenu.Items.Add(new ToolStripMenuItem("Copy", null, (object? sender, EventArgs e) => Copy())
                {
                    Enabled = !string.IsNullOrEmpty(this.SelectedText)
                });

                _contextMenu.Items.Add(new ToolStripMenuItem("Paste", null, (object? sender, EventArgs e) => Paste())
                {
                    Enabled = Clipboard.ContainsText()
                });

                _contextMenu.Items.Add(new ToolStripSeparator());

                _contextMenu.Items.Add(new ToolStripMenuItem("Expand All Folds", null, (object? sender, EventArgs e) => ExpandAllFolds())
                {
                    Enabled = _foldingManager.AllFoldings.Count() > 0
                });

                _contextMenu.Items.Add(new ToolStripMenuItem("Collapse All Folds", null, (object? sender, EventArgs e) => CollapseAllFolds())
                {
                    Enabled = _foldingManager.AllFoldings.Count() > 0
                });

                var location = e.GetPosition(this);
                _contextMenu.Show(_controlHost.EnsureNotNull(), (int)location.X, (int)location.Y);
            }
            catch
            {
            }
        }

        private void Editor_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.Key == System.Windows.Input.Key.F)
            {
                CodeTabPage.StudioForm.ShowFind();
            }
            else if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.Key == System.Windows.Input.Key.H)
            {
                CodeTabPage.StudioForm.ShowReplace();
            }
            else if (e.Key == System.Windows.Input.Key.F3)
            {
                bool forceShowFind = (Control.ModifierKeys & Keys.Control) == Keys.Control;

                CodeTabPage.StudioForm.FindNext(forceShowFind);
            }
        }

        private void Editor_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var files = e.Data?.GetData(DataFormats.FileDrop, false) as string[];
            if (files != null)
            {
                foreach (var file in files)
                {
                    CodeTabPage.StudioForm.OpenFileOrSelectExisting(file);
                }
            }
        }

        private void Editor_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
    }
}
