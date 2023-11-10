using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NTDLS.Katzebase.UI.Controls;
using System.Xml;

namespace NTDLS.Katzebase.UI.Classes
{
    internal class EditorFactory
    {

#if DEBUG
        private static string sqlHighlighter => @"..\..\..\..\..\NTDLS.Katzebase.Server\@Installers\Highlighters\KBS.xshd";
#else
        private static string sqlHighlighter => Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "Highlighters", "KBS.xshd");
#endif

        public TabControl TabControlParent { get; private set; }
        private readonly FormStudio _form;

        public EditorFactory(FormStudio form, TabControl tabControl)
        {
            TabControlParent = tabControl;

            TabControlParent.Click += TabControlParent_Click;
            TabControlParent.TabIndexChanged += TabControlParent_TabIndexChanged;

            _form = form;
        }

        private void TabControlParent_TabIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                if (sender is TabControl tabControl)
                {
                    ((TabFilePage)tabControl.SelectedTab).Editor.Focus();
                }
            }
            catch
            {
            }
        }

        private void TabControlParent_Click(object? sender, EventArgs e)
        {
            try
            {
                if (sender is TabControl tabControl)
                {
                    ((TabFilePage)tabControl.SelectedTab).Editor.Focus();
                }
            }
            catch
            {
            }
        }

        public TabFilePage Create(string serverAddressURL, string tabText)
        {
            var editor = new TextEditor
            {
                ShowLineNumbers = true,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 16f,
                WordWrap = false,
            };

            var tabFilePage = new TabFilePage(TabControlParent, serverAddressURL, tabText, editor)
            {
                //..
            };

            editor.Tag = tabFilePage;

            if (File.Exists(sqlHighlighter))
            {
                using XmlReader reader = XmlReader.Create(sqlHighlighter);
                editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                reader.Close();
            }

            editor.TextChanged += TextEditor_TextChanged;
            editor.DragEnter += Editor_DragEnter;
            editor.Drop += Editor_Drop;
            editor.KeyUp += Editor_KeyUp;

            TabControlParent.TabPages.Add(tabFilePage);
            TabControlParent.SelectedTab = tabFilePage;

            return tabFilePage;
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

        private TabFilePage? EditorToTab(TextEditor editor)
        {
            foreach (TabFilePage tab in TabControlParent.TabPages)
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
                    if (draggedNode.Tag is string text && sender is TextEditor editor)
                    {
                        var position = editor.GetPositionFromPoint(e.GetPosition(editor));
                        if (position == null)
                        {
                            position = new TextViewPosition(1, 1);
                        }
                        else if (position != null)
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
