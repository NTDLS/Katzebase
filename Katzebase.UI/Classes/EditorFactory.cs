using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;

namespace Katzebase.UI.Classes
{
    internal class EditorFactory
    {

#if DEBUG
        private static string sqlHighlighter => @"C:\NTDLS\Katzebase\Installers\Syntax Highlighters\SQL.xshd";
        private static string configHighlighter = @"C:\NTDLS\Katzebase\Installers\Syntax Highlighters\Config.xshd";
#else
        private static string sqlHighlighter => Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "Highlighters", "SQL.xshd");
        private static string configHighlighter = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "Highlighters", "Config.xshd"); //Workload generator highlighter.
#endif

        private TabControl _tabControl;
        private FormStudio _form;

        public EditorFactory(FormStudio form, TabControl tabControl)
        {
            _tabControl = tabControl;
            _form = form;
        }

        public TextEditor Create(ProjectTreeNode forNode)
        {
            var editor = new TextEditor
            {
                ShowLineNumbers = true,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 16f,
                WordWrap = false,
                Tag = forNode
            };

            if (forNode.NodeType == Constants.ProjectNodeType.Script)
            {
                if (File.Exists(sqlHighlighter))
                {
                    using XmlReader reader = XmlReader.Create(sqlHighlighter);
                    editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    reader.Close();
                }

                if (File.Exists(forNode.FullFilePath))
                {
                    editor.Document.FileName = forNode.FullFilePath;
                    editor.Text = File.ReadAllText(editor.Document.FileName);
                }
            }
            else if (forNode.NodeType == Constants.ProjectNodeType.Workloads || forNode.NodeType == Constants.ProjectNodeType.Workload)
            {
                if (File.Exists(configHighlighter))
                {
                    using XmlReader reader = XmlReader.Create(configHighlighter);
                    editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    reader.Close();
                }

                if (File.Exists(forNode.ConfigFilePath))
                {
                    editor.Document.FileName = forNode.ConfigFilePath;
                    editor.Text = File.ReadAllText(editor.Document.FileName);
                }
            }
            else if (File.Exists(forNode.FullFilePath))
            {
                editor.Text = File.ReadAllText(forNode.FullFilePath);
            }

            editor.TextChanged += TextEditor_TextChanged;
            editor.DragEnter += Editor_DragEnter;
            editor.Drop += Editor_Drop;
            editor.KeyUp += Editor_KeyUp;

            return editor;
        }

        private void Editor_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F5)
            {
                //_form.PreviewCurrentTab();
            }
            else if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.Key == System.Windows.Input.Key.F)
            {
                _form.ShowFind();
            }
            else if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.Key == System.Windows.Input.Key.H)
            {
                _form.ShowReplace();
            }
            else if (e.Key == System.Windows.Input.Key.F3)
            {
                _form.FindNext();
            }
        }

        public static TextEditor CreateGeneric(string text = "")
        {
            var editor = new TextEditor
            {
                ShowLineNumbers = true,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 16f,
                WordWrap = false
            };

            if (File.Exists(sqlHighlighter))
            {
                using XmlReader reader = XmlReader.Create(sqlHighlighter);
                editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                reader.Close();
            }

            editor.Text = text;

            return editor;
        }

        private ProjectTabPage? EditorToTab(TextEditor editor)
        {
            foreach (ProjectTabPage tab in _tabControl.TabPages)
            {
                if (tab.Editor == editor)
                {
                    return tab;
                }
            }
            return null;
        }

        private void TextEditor_TextChanged(object? sender, EventArgs e)
        {
            var editor = sender as TextEditor;
            if (editor != null)
            {
                var tab = EditorToTab(editor);
                if (tab != null)
                {
                    if (tab.Text.EndsWith('*') == false)
                    {
                        tab.Text = $"{tab.Text}*";
                    }
                    tab.IsSaved = false;
                }
            }
        }

        private void Editor_Drop(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                var draggedNode = e.Data.GetData(typeof(TreeNode)) as TreeNode;
                if (draggedNode != null && draggedNode.Tag != null)
                {
                    string? text = draggedNode.Tag as string;
                    if (text != null && sender is TextEditor editor)
                    {
                        var position = editor.GetPositionFromPoint(e.GetPosition(editor));
                        if (position == null)
                        {
                            position = new TextViewPosition(1, 1);
                        }
                        if (position != null)
                        {
                            var docLine = editor.Document.GetLineByNumber(position.Value.Line);

                            editor.CaretOffset = docLine.Offset + (position.Value.Column - 1);

                            int caretOffset = editor.CaretOffset;
                            editor.Document.Insert(caretOffset, text);
                        }

                        editor.Focus();
                    }
                }
            }
            catch
            {
                //I've never seen an exception, but I dont want anyone losing work if my math is off.
            }
        }

        private void Editor_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
    }
}
