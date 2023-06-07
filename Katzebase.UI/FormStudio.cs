using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Katzebase.UI.Classes;
using Katzebase.UI.Properties;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace Katzebase.UI
{
    public partial class FormStudio : Form
    {
        private int _executionExceptionCount = 0;
        private bool _timerTicking = false;
        private bool _firstShown = true;
        private readonly EditorFactory? _editorFactory = null;
        private readonly ImageList _treeImages = new ImageList();
        private readonly System.Windows.Forms.Timer _toolbarSyncTimer = new();
        private bool _scriptExecuting = false;
        public string _lastusedServerAddress = string.Empty;

        public FormStudio()
        {
            InitializeComponent();
            _editorFactory = new EditorFactory(this, this.tabControlBody);
        }

        public FormStudio(string projectFile)
        {
            InitializeComponent();
            _editorFactory = new EditorFactory(this, this.tabControlBody);
        }

        private void FormStudio_Load(object sender, EventArgs e)
        {
            treeViewProject.Dock = DockStyle.Fill;
            splitContainerProject.Dock = DockStyle.Fill;
            splitContainerMacros.Dock = DockStyle.Fill;
            tabControlBody.Dock = DockStyle.Fill;
            treeViewMacros.Dock = DockStyle.Fill;

            _treeImages.ColorDepth = ColorDepth.Depth32Bit;
            _treeImages.Images.Add("Folder", Resources.TreeFolder);
            _treeImages.Images.Add("Server", Resources.TreeServer);
            _treeImages.Images.Add("Schema", Resources.TreeSchema);
            _treeImages.Images.Add("Index", Resources.TreeIndex);
            _treeImages.Images.Add("IndexFolder", Resources.TreeIndexFolder);
            _treeImages.Images.Add("TreeNotLoaded", Resources.TreeNotLoaded);
            treeViewProject.ImageList = _treeImages;


            treeViewProject.BeforeExpand += TreeViewProject_BeforeExpand;
            treeViewProject.NodeMouseClick += TreeViewProject_NodeMouseClick;

            treeViewMacros.ShowNodeToolTips = true;
            //treeViewMacros.Nodes.AddRange(...);
            treeViewMacros.ItemDrag += TreeViewMacros_ItemDrag;

            this.Shown += FormStudio_Shown;
            this.FormClosing += FormStudio_FormClosing;

            tabControlBody.MouseUp += TabControlBody_MouseUp;

            splitContainerOutput.Panel2Collapsed = true; //For now, we just hide the bottom panel since we dont really do debugging.
            splitContainerMacros.Panel2Collapsed = true;

            _toolbarSyncTimer.Tick += _toolbarSyncTimer_Tick;
            _toolbarSyncTimer.Interval = 250;
            _toolbarSyncTimer.Start();
        }

        private void _toolbarSyncTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                lock (this)
                {
                    if (_timerTicking)
                    {
                        return;
                    }
                    _timerTicking = true;
                }

                SyncToolbarAndMenuStates();

                _timerTicking = false;
            }
            catch { }
        }

        private void SyncToolbarAndMenuStates()
        {
            var tabFilePage = CurrentTabFilePage();

            bool isTabOpen = (tabFilePage != null);
            bool isTextSelected = (tabFilePage != null) && (tabFilePage?.Editor?.SelectionLength > 0);

            toolStripButtonCloseCurrentTab.Enabled = isTabOpen;
            toolStripButtonCopy.Enabled = isTextSelected;
            toolStripButtonCut.Enabled = isTextSelected;
            toolStripButtonFind.Enabled = isTabOpen;
            toolStripButtonPaste.Enabled = isTabOpen;
            toolStripButtonRedo.Enabled = isTabOpen;
            toolStripButtonReplace.Enabled = isTabOpen;
            toolStripButtonExecuteScript.Enabled = isTabOpen && !_scriptExecuting;
            toolStripButtonExplainPlan.Enabled = isTabOpen && !_scriptExecuting;
            toolStripButtonUndo.Enabled = isTabOpen;

            toolStripButtonDecreaseIndent.Enabled = isTextSelected;
            toolStripButtonIncreaseIndent.Enabled = isTextSelected;

            toolStripButtonSave.Enabled = isTabOpen;
            toolStripButtonSaveAll.Enabled = isTabOpen;
            toolStripButtonSnippets.Enabled = isTabOpen;
        }

        #region Form events.

        private void FormStudio_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (CloseAllTabs() == false)
            {
                e.Cancel = true;
            }
        }

        private void FormStudio_Shown(object? sender, EventArgs e)
        {
            if (_firstShown == false)
            {
                return;
            }

            if (_firstShown)
            {
                _firstShown = false;

                Connect();
            }

            SyncToolbarAndMenuStates();
        }

        #endregion

        #region Project Treeview Shenanigans.

        private void FlattendTreeViewNodes(ref List<ServerTreeNode> flatList, ServerTreeNode parent)
        {
            foreach (var node in parent.Nodes.Cast<ServerTreeNode>())
            {
                flatList.Add(node);
                FlattendTreeViewNodes(ref flatList, node);
            }
        }

        private ServerTreeNode? GetProjectAssetsNode(ServerTreeNode startNode)
        {
            foreach (var node in startNode.Nodes.Cast<ServerTreeNode>())
            {
                if (node.Text == "Assets")
                {
                    return node;
                }
                var result = GetProjectAssetsNode(node);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }


        private void TreeViewProject_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var popupMenu = new ContextMenuStrip();
            treeViewProject.SelectedNode = e.Node;

            popupMenu.ItemClicked += PopupMenu_ItemClicked;

            var node = e.Node as ServerTreeNode;
            if (node == null)
            {
                throw new Exception("Invalid node type.");
            }

            popupMenu.Tag = e.Node as ServerTreeNode;

            if (node.NodeType == Classes.Constants.ServerNodeType.Server)
            {
                popupMenu.Items.Add("Refresh", FormUtility.TransparentImage(Resources.ToolFind));
            }
            else if (node.NodeType == Classes.Constants.ServerNodeType.Schema)
            {
                popupMenu.Items.Add("Create Index", FormUtility.TransparentImage(Resources.Asset));
                popupMenu.Items.Add("Select top n...", FormUtility.TransparentImage(Resources.Workload));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Delete", FormUtility.TransparentImage(Resources.Asset));
            }
            else if (node.NodeType == Classes.Constants.ServerNodeType.IndexFolder)
            {
                popupMenu.Items.Add("Create Index", FormUtility.TransparentImage(Resources.Asset));
            }
            else if (node.NodeType == Classes.Constants.ServerNodeType.Index)
            {
                popupMenu.Items.Add("Delete Index", FormUtility.TransparentImage(Resources.Asset));
            }

            popupMenu.Show(treeViewProject, e.Location);
        }

        private void PopupMenu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            var menuStrip = sender as ContextMenuStrip;
            Utility.EnsureNotNull(menuStrip);

            menuStrip.Close();

            Utility.EnsureNotNull(menuStrip.Tag);

            var node = (menuStrip.Tag) as ServerTreeNode;
            Utility.EnsureNotNull(node);

            if (e.ClickedItem?.Text == "Refresh")
            {
                //TODO: Refresh schema?
            }
            else if (e.ClickedItem?.Text == "Delete")
            {
                /*
                var messageBoxResult = MessageBox.Show($"Delete {node.Text}?", $"Delete {node.NodeType}?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (messageBoxResult == DialogResult.Yes)
                {
                    node.Remove();
                }
                */
            }
            else if (e.ClickedItem?.Text == "Select top n...")
            {
                var rootNode = TreeManagement.GetRootNode(node);
                var tabFilePage = AddTab(FormUtility.GetNextNewFileName(), rootNode.ServerAddress);
                tabFilePage.Editor.Text = "SET TraceWaitTimes ON;\r\n\r\nSELECT TOP 100\r\n\t*\r\nFROM\r\n\t" + TreeManagement.CalculateFullSchema(node);
            }
        }

        #endregion

        #region Project Treeview Events.

        private void TreeViewProject_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null && (e.Node as ServerTreeNode)?.NodeType == Classes.Constants.ServerNodeType.Schema)
            {
                TreeManagement.PopulateSchemaNodeOnExpand(treeViewProject, (ServerTreeNode)e.Node);
            }
        }

        #endregion

        #region Macros Treeview Bullshit.

        private void TreeViewMacros_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item != null)
            {
                DoDragDrop(e.Item, DragDropEffects.All);
            }
        }

        #endregion

        #region Body Tab Magic.

        private void TabControlBody_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var clickedTab = GetClickedTab(e.Location);
                if (clickedTab == null)
                {
                    return;
                }

                var popupMenu = new ContextMenuStrip();
                popupMenu.ItemClicked += popupMenu_tabControlScripts_MouseUp_ItemClicked;

                popupMenu.Tag = clickedTab;

                popupMenu.Items.Add("Close", FormUtility.TransparentImage(Properties.Resources.ToolCloseFile));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Close all but this", FormUtility.TransparentImage(Properties.Resources.ToolCloseFile));
                popupMenu.Items.Add("Close all", FormUtility.TransparentImage(Properties.Resources.ToolCloseFile));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Find in project", FormUtility.TransparentImage(Properties.Resources.ToolFind));
                popupMenu.Items.Add("Open containing folder", FormUtility.TransparentImage(Properties.Resources.ToolOpenFile));
                popupMenu.Show(tabControlBody, e.Location);
            }
        }

        private void popupMenu_tabControlScripts_MouseUp_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            var contextMenu = sender as ContextMenuStrip;
            if (contextMenu == null)
            {
                return;
            }

            contextMenu.Hide();

            ToolStripItem? clickedItem = e?.ClickedItem;
            if (clickedItem == null)
            {
                return;
            }

            TabFilePage? clickedTab = contextMenu.Tag as TabFilePage;
            if (clickedTab == null)
            {
                return;
            }

            if (clickedItem.Text == "Close")
            {
                CloseTab(clickedTab);
            }
            else if (clickedItem.Text == "Open containing folder")
            {
                if (clickedTab != null)
                {
                    var directory = clickedTab.FilePath;

                    if (Directory.Exists(clickedTab.FilePath) == false)
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
            else if (clickedItem.Text == "Close all but this")
            {
                var tabsToClose = new List<TabFilePage>();

                //Minimize the number of "SelectedIndexChanged" events that get fired.
                //We get a big ol' thread exception when we dont do this. Looks like an internal control exception.
                tabControlBody.SelectedTab = clickedTab;
                System.Windows.Forms.Application.DoEvents(); //Make sure the message pump can actually select the tab before we start closing.

                foreach (var tabFilePage in tabControlBody.TabPages.Cast<TabFilePage>())
                {
                    if (tabFilePage != clickedTab)
                    {
                        tabsToClose.Add(tabFilePage);
                    }
                }

                foreach (var tabFilePage in tabsToClose)
                {
                    if (CloseTab(tabFilePage) == false)
                    {
                        break;
                    }
                }
            }
            else if (clickedItem.Text == "Close all")
            {
                CloseAllTabs();
            }

            //UpdateToolbarButtonStates();
        }

        private TabFilePage? GetClickedTab(Point mouseLocation)
        {
            for (int i = 0; i < tabControlBody.TabCount; i++)
            {
                Rectangle r = tabControlBody.GetTabRect(i);
                if (r.Contains(mouseLocation))
                {
                    return (TabFilePage)tabControlBody.TabPages[i];
                }
            }
            return null;
        }

        private TabFilePage AddTab(string filePath, string serverAddress)
        {
            Utility.EnsureNotNull(_editorFactory);

            var tabFilePage = _editorFactory.Create(serverAddress, filePath);

            tabFilePage.Editor.KeyUp += Editor_KeyUp;

            tabFilePage.Controls.Add(new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = tabFilePage.Editor
            });
            tabControlBody.TabPages.Add(tabFilePage);
            tabControlBody.SelectedTab = tabFilePage;

            return tabFilePage;
        }

        private void Editor_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F5)
            {
                ExecuteCurrentScriptAsync(false);
            }
        }

        /// <summary>
        /// Removes a tab, saved or not - no prompting.
        /// </summary>
        /// <param name="tab"></param>
        private void RemoveTab(TabFilePage? tab)
        {
            if (tab != null)
            {
                tabControlBody.TabPages.Remove(tab);
            }
            SyncToolbarAndMenuStates();
        }

        bool Connect()
        {
            try
            {
                using var form = new FormConnect();
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _lastusedServerAddress = form.ServerAddressURL;

                    var tabFilePage = AddTab(FormUtility.GetNextNewFileName(), _lastusedServerAddress);
                    tabFilePage.Editor.Text = "SET TraceWaitTimes ON;\r\n\r\nSELECT TOP 100\r\n\tProductID, LocationID, Shelf,\r\n\tBin, Quantity, rowguid, ModifiedDate\r\nFROM\r\n\tAdventureWorks2012:Production:ProductInventory\r\nWHERE\r\n\t(\r\n\t\tLocationId = 6\r\n\t\tAND Shelf != 'R'\r\n\t\tAND Quantity = 299\r\n\t)\r\n\tOR\r\n\t(\r\n\t\t(\r\n\t\t\tLocationId = 6\r\n\t\t\tAND Shelf != 'M'\r\n\t\t)\r\n\t\tAND Quantity = 299\r\n\t\tOR ProductId = 366\r\n\t)\r\n\tAND\r\n\t(\r\n\t\tBIN = 8\r\n\t\tOR Bin = 11\r\n\t\tOR Bin = 19\r\n\t)\r\n";
                    //tabFilePage.Editor.Text = "SET TraceWaitTimes ON;\r\n\r\nSELECT TOP 100\r\n\t*\r\nFROM\r\n\tAdventureWorks2012:Production:ProductInventory\r\n";
                    TreeManagement.PopulateServer(treeViewProject, _lastusedServerAddress);

                    foreach (TreeNode node in treeViewProject.Nodes)
                    {
                        node.Expand();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, PublicLibrary.Constants.FriendlyName);
            }

            return false;
        }

        bool Disconnect()
        {
            if (CloseAllTabs() == false)
            {
                return false;
            }

            treeViewProject.Nodes.Clear();

            return true;
        }

        bool CloseAllTabs()
        {
            //Minimize the number of "SelectedIndexChanged" events that get fired.
            //We get a big ol' thread exception when we dont do this. Looks like an internal control exception.
            tabControlBody.SelectedIndex = 0;
            System.Windows.Forms.Application.DoEvents(); //Make sure the message pump can actually select the tab before we start closing.

            tabControlBody.SuspendLayout();

            bool result = true;
            while (tabControlBody.TabPages.Count != 0)
            {
                if (!CloseTab(tabControlBody.TabPages[tabControlBody.TabPages.Count - 1] as TabFilePage))
                {
                    result = false;
                    break;
                }
            }

            SyncToolbarAndMenuStates();

            tabControlBody.ResumeLayout();

            return result;
        }

        /// <summary>
        /// Usser friendly tab close.
        /// </summary>
        /// <param name="tab"></param>
        private bool CloseTab(TabFilePage? tab)
        {
            if (tab != null)
            {
                if (tab.IsSaved == false)
                {
                    var messageBoxResult = MessageBox.Show("Save " + tab.Text + " before closing?", "Save File?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (messageBoxResult == DialogResult.Yes)
                    {
                        tab.Save();
                    }
                    else if (messageBoxResult == DialogResult.No)
                    {
                    }
                    else //Cancel and otherwise.
                    {
                        SyncToolbarAndMenuStates();
                        return false;
                    }
                }

                RemoveTab(tab);
            }

            SyncToolbarAndMenuStates();
            return true;
        }

        private TabFilePage? CurrentTabFilePage()
        {
            var currentTab = tabControlBody.SelectedTab as TabFilePage;
            if (currentTab?.Editor != null)
            {
                return (TabFilePage)currentTab.Editor.Tag;
            }
            return null;
        }

        #endregion

        #region Toolbar Clicks.

        private void toolStripButtonExecuteScript_Click(object sender, EventArgs e)
        {
            ExecuteCurrentScriptAsync(false);
        }

        private void toolStripButtonExplainPlan_Click(object sender, EventArgs e)
        {
            ExecuteCurrentScriptAsync(true);
        }

        private void toolStripButtonCloseCurrentTab_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            CloseTab(selection);
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            if (selection == null)
            {
                return;
            }
            selection.Save();
        }

        private void toolStripButtonSaveAll_Click(object sender, EventArgs e)
        {
            foreach (var tabFilePage in tabControlBody.TabPages.Cast<TabFilePage>())
            {
                tabFilePage.Save();
            }
        }

        private void toolStripButtonFind_Click(object sender, EventArgs e)
        {
            ShowFind();
        }

        private void toolStripButtonReplace_Click(object sender, EventArgs e)
        {
            ShowReplace();
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Redo();
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Undo();
        }

        private void toolStripButtonCut_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Cut();
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Copy();
        }

        private void toolStripButtonPaste_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Paste();
        }

        private void toolStripButtonIncreaseIndent_Click(object sender, EventArgs e)
        {
            IncreaseCurrentTabIndent();
        }

        public void IncreaseCurrentTabIndent()
        {
            var tabFilePage = CurrentTabFilePage();
            if (tabFilePage != null)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void toolStripButtonDecreaseIndent_Click(object sender, EventArgs e)
        {
            DecreaseCurrentTabIndent();
        }

        public void DecreaseCurrentTabIndent()
        {
            SendKeys.Send("+({TAB})");
        }

        private void toolStripButtonMacros_Click(object sender, EventArgs e)
        {
            splitContainerMacros.Panel2Collapsed = !splitContainerMacros.Panel2Collapsed;
        }

        private void toolStripButtonProject_Click(object sender, EventArgs e)
        {
            splitContainerProject.Panel1Collapsed = !splitContainerProject.Panel1Collapsed;
        }

        public void ShowReplace()
        {
            var info = CurrentTabFilePage();
            if (info != null)
            {
                info.ReplaceTextForm.ShowDialog();
            }
        }

        public void ShowFind()
        {
            var info = CurrentTabFilePage();
            if (info != null)
            {
                info.FindTextForm.ShowDialog();
            }
        }

        public void FindNext()
        {
            var info = CurrentTabFilePage();
            if (info != null)
            {
                if (string.IsNullOrEmpty(info.FindTextForm.SearchText))
                {
                    info.FindTextForm.ShowDialog();
                }
                else
                {
                    info.FindTextForm.FindNext();
                }
            }
        }

        private void Group_OnStatus(WorkloadGroup sender, string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<WorkloadGroup, string, Color>(Group_OnStatus), sender, text, color);
                return;
            }

            AppendToOutput(text, color);
        }

        private void AppendToExplain(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, Color>(AppendToOutput), text, color);
                return;
            }

            richTextBoxExplain.SelectionStart = richTextBoxExplain.TextLength;
            richTextBoxExplain.SelectionLength = 0;

            richTextBoxExplain.SelectionColor = color;
            richTextBoxExplain.AppendText($"{text}\r\n");
            richTextBoxExplain.SelectionColor = richTextBoxExplain.ForeColor;

            richTextBoxExplain.SelectionStart = richTextBoxExplain.Text.Length;
            richTextBoxExplain.ScrollToCaret();
        }

        private void AppendToOutput(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, Color>(AppendToOutput), text, color);
                return;
            }

            richTextBoxOutput.SelectionStart = richTextBoxOutput.TextLength;
            richTextBoxOutput.SelectionLength = 0;

            richTextBoxOutput.SelectionColor = color;
            richTextBoxOutput.AppendText($"{text}\r\n");
            richTextBoxOutput.SelectionColor = richTextBoxOutput.ForeColor;

            richTextBoxOutput.SelectionStart = richTextBoxOutput.Text.Length;
            richTextBoxOutput.ScrollToCaret();
        }

        private void Group_OnException(WorkloadGroup sender, KbExceptionBase ex)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<WorkloadGroup, KbExceptionBase>(Group_OnException), sender, ex);
                return;
            }

            _executionExceptionCount++;

            splitContainerOutput.Panel2Collapsed = false;

            AppendToOutput($"Exception: {ex.Message}\r\n", Color.DarkRed);
        }

        private void toolStripButtonOutput_Click(object sender, EventArgs e)
        {
            splitContainerOutput.Panel2Collapsed = !splitContainerOutput.Panel2Collapsed;
        }

        private void toolStripButtonSnippets_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            if (tabFilePage != null)
            {

                using (var form = new FormSnippets())
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        tabFilePage.Editor.Document.Insert(tabFilePage.Editor.CaretOffset, form.SelectedSnippetText);
                    }
                }
            }
        }

        #endregion

        #region Form Menu.

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            if (selection == null)
            {
                return;
            }
            selection.Save();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            CloseTab(selection);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new FormAbout())
            {
                form.ShowDialog();
            }
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var tabFilePage in tabControlBody.TabPages.Cast<TabFilePage>())
            {
                tabFilePage.Save();
            }
        }

        private void toolStripButtonNewProject_Click(object sender, EventArgs e)
        {
            var tabFilePage = AddTab(FormUtility.GetNextNewFileName(), _lastusedServerAddress);
            tabFilePage.Editor.Text = "SET TraceWaitTimes OFF;\r\n\r\n";
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseAllTabs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Execute Current Script.

        /// <summary>
        /// This is for actually executing the script against a live database.
        /// </summary>
        private void ExecuteCurrentScriptAsync(bool justExplain)
        {
            if (_scriptExecuting)
            {
                return;
            }
            _scriptExecuting = true;

            var tabFilePage = CurrentTabFilePage();
            if (tabFilePage == null)
            {
                return;
            }
            tabFilePage.Save();

            PreExecuteEvent(tabFilePage);

            dataGridViewResults.Rows.Clear();
            dataGridViewResults.Columns.Clear();

            string scriptText = tabFilePage.Editor.Text;

            Task.Run(() =>
            {
                ExecuteCurrentScriptSync(tabFilePage.Client, scriptText, justExplain);
            }).ContinueWith((t) =>
            {
                PostExecuteEvent(tabFilePage);
            });
        }

        private void PreExecuteEvent(TabFilePage tabFilePage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<TabFilePage>(PreExecuteEvent), tabFilePage);
                return;
            }

            richTextBoxOutput.Text = "";
            richTextBoxExplain.Text = "";
            _executionExceptionCount = 0;

            splitContainerOutput.Panel2Collapsed = false;
        }

        private void PostExecuteEvent(TabFilePage tabFilePage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<TabFilePage>(PostExecuteEvent), tabFilePage);
                return;
            }

            splitContainerOutput.Panel2Collapsed = false;

            if (dataGridViewResults.RowCount > 0)
            {
                tabControlOutput.SelectedTab = tabPageResults;
            }
            else
            {
                tabControlOutput.SelectedTab = tabPageOutput;
            }

            _scriptExecuting = false;
        }

        private void ExecuteCurrentScriptSync(KatzebaseClient client, string scriptText, bool justExplain)
        {
            WorkloadGroup group = new WorkloadGroup();

            try
            {
                group.OnException += Group_OnException;
                group.OnStatus += Group_OnStatus;

                var scripts = scriptText.Split(";"); //TODO: This needs to be MUCH more intelligent!

                foreach (var script in scripts)
                {
                    DateTime startTime = DateTime.UtcNow;

                    KbQueryResult result;

                    if (justExplain)
                    {
                        result = client.Query.ExplainQuery(script);
                    }
                    else
                    {
                        result = client.Query.ExecuteQuery(script);
                    }

                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    AppendToOutput($"Execution completed in {duration:N0}ms.", Color.Black);

                    if (justExplain && string.IsNullOrWhiteSpace(result.Explanation) == false)
                    {
                        AppendToOutput(result.Explanation, Color.DarkGreen);
                    }

                    if (result.WaitTimes.Count > 0)
                    {
                        var waitTimeTotal = result.WaitTimes.Sum(o => o.Value);

                        var waitTimes = new StringBuilder();
                        waitTimes.AppendLine("Trace wait times {");
                        foreach (var wt in result.WaitTimes.Where(o => o.Value > 0.5).OrderBy(o => o.Value))
                        {
                            waitTimes.AppendLine($"\t{wt.Name}: {wt.Value:n0}");
                        }
                        waitTimes.AppendLine($"}} = {waitTimeTotal:n0}ms");

                        AppendToOutput(waitTimes.ToString(), Color.DarkBlue);
                    }

                    PopulateResultsGrid(result);

                    if (string.IsNullOrWhiteSpace(result.Message) == false)
                    {
                        AppendToOutput($"{result.Message}", Color.Black);
                    }
                }
            }
            catch (KbExceptionBase ex)
            {
                Group_OnException(group, ex);
            }
            catch (Exception ex)
            {
                Group_OnException(group, new KbExceptionBase(ex.Message));
            }
        }

        private void PopulateResultsGrid(KbQueryResult result)
        {
            if (result.Rows.Count == 0)
            {
                return;
            }

            if (InvokeRequired)
            {
                Invoke(new Action<KbQueryResult>(PopulateResultsGrid), result);
                return;
            }

            dataGridViewResults.SuspendLayout();

            foreach (var field in result.Fields)
            {
                dataGridViewResults.Columns.Add(field.Name, field.Name);
            }

            int maxRowsToLoad = 100;
            foreach (var row in result.Rows)
            {
                var rowValues = new List<string>();

                for (int fieldIndex = 0; fieldIndex < result.Fields.Count; fieldIndex++)
                {
                    var fieldValue = row.Values[fieldIndex];
                    rowValues.Add(fieldValue ?? string.Empty);
                }

                dataGridViewResults.Rows.Add(rowValues.ToArray());

                maxRowsToLoad--;
                if (maxRowsToLoad <= 0)
                {
                    break;
                }
            }

            dataGridViewResults.ResumeLayout();
        }

        #endregion
    }
}
