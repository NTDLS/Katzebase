using ICSharpCode.AvalonEdit;
using System.Diagnostics;
using Katzebase.UI.Classes;

namespace Katzebase.UI
{
    public partial class FormSnippets : Form
    {
#if DEBUG
        private static string _snippetsPath => @"C:\NTDLS\Katzebase\Installers\Snippets";
#else
        private static string _snippetsPath => Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "Snippets");
#endif

        public string SelectedSnippetText { get; set; } = string.Empty;

        private TextEditor? _editor;

        public FormSnippets()
        {
            InitializeComponent();
        }

        private void FormSnippets_Load(object sender, EventArgs e)
        {
            _editor = EditorFactory.CreateGeneric();

            var host = new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = _editor
            };
            splitContainerBody.Panel2.Controls.Add(host);

            var rootNode = LoadSnippets(_snippetsPath);

            rootNode.Expand();

            treeViewSnippets.Nodes.Add(rootNode);
            treeViewSnippets.NodeMouseDoubleClick += TreeViewSnippets_NodeMouseDoubleClick;
            treeViewSnippets.NodeMouseClick += TreeViewSnippets_NodeMouseClick;
            treeViewSnippets.KeyUp += TreeViewSnippets_KeyUp;
            treeViewSnippets.NodeMouseClick += TreeViewSnippets_NodeMouseClick1;
        }

        private void TreeViewSnippets_NodeMouseClick1(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (e.Node == null)
                {
                    return;
                }

                var popupMenu = new ContextMenuStrip();
                popupMenu.ItemClicked += PopupMenu_ItemClicked;

                popupMenu.Tag = e.Node;

                popupMenu.Items.Add("Open containing folder", FormUtility.TransparentImage(Properties.Resources.ToolOpenFile));
                popupMenu.Show(treeViewSnippets, e.Location);
            }
        }

        private void PopupMenu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            var contextMenu = sender as ContextMenuStrip;
            if (contextMenu == null)
            {
                return;
            }

            ToolStripItem? clickedItem = e?.ClickedItem;
            if (clickedItem == null)
            {
                return;
            }

            TreeNode? clickedNode = contextMenu.Tag as TreeNode;
            if (clickedNode == null)
            {
                return;
            }

            if (clickedItem.Text == "Open containing folder")
            {
                if (clickedNode != null)
                {
                    var directory = clickedNode.Tag as string;

                    if (Directory.Exists(directory) == false)
                    {
                        directory = Path.GetDirectoryName(directory);
                    }

                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = directory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
        }

        private void TreeViewSnippets_KeyUp(object? sender, KeyEventArgs e)
        {
            try
            {
                if (_editor != null && treeViewSnippets?.SelectedNode?.Tag != null)
                {
                    _editor.Text = File.ReadAllText((string)treeViewSnippets.SelectedNode.Tag);
                }
            }
            catch { }
        }

        private void TreeViewSnippets_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                if (_editor != null && e?.Node?.Tag != null)
                {
                    var filePath = e.Node.Tag as string;
                    if (File.Exists(filePath))
                    {
                        _editor.Text = File.ReadAllText(filePath);
                    }
                }
            }
            catch { }
        }

        private void TreeViewSnippets_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                if (e?.Node?.Tag != null)
                {
                    SelectedSnippetText = File.ReadAllText((string)e.Node.Tag).Trim();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch { }
        }

        private TreeNode LoadSnippets(string path, TreeNode? node = null)
        {
            TreeNode thisNode;

            if (node == null)
            {
                node = new TreeNode(Path.GetFileName(path))
                {
                    Tag = _snippetsPath
                };
                thisNode = node;
            }
            else
            {
                thisNode = node.Nodes.Add(Path.GetFileName(path));
            }

            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                LoadSnippets(directory, thisNode);
            }

            var files = Directory.GetFiles(path, "*.txt");
            foreach (var file in files)
            {
                thisNode.Nodes.Add(Path.GetFileNameWithoutExtension(file)).Tag = file;
            }

            return thisNode;
        }
    }
}
