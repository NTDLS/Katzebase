using NTDLS.Helpers;
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Management.Classes;
using NTDLS.Katzebase.Management.Classes.Editor;
using NTDLS.Katzebase.Management.Controls;
using NTDLS.Katzebase.Management.Properties;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Management.Classes.Editor.AutoCompleteFunction;
using static NTDLS.Katzebase.Management.Controls.CodeEditorTabPage;

namespace NTDLS.Katzebase.Management
{
    public partial class FormStudio : Form
    {
        private readonly ImageList _treeImages = new();
        private readonly System.Windows.Forms.Timer _toolbarSyncTimer = new();
        private readonly string _firstLoadFilename = string.Empty;
        private readonly ServerExplorerManager _serverExplorerManager;

        private bool _timerTicking = false;
        private bool _firstShown = true;

        public FormStudio()
        {
            InitializeComponent();
            Text = KbConstants.FriendlyName;
            _serverExplorerManager = new ServerExplorerManager(this, treeViewServerExplorer);
        }

        public FormStudio(string firstLoadFilename)
        {
            InitializeComponent();
            _firstLoadFilename = firstLoadFilename;
            Text = KbConstants.FriendlyName;
            _serverExplorerManager = new ServerExplorerManager(this, treeViewServerExplorer);
        }

        private void FormStudio_Load(object sender, EventArgs e)
        {
            _treeImages.ColorDepth = ColorDepth.Depth32Bit;
            _treeImages.Images.Add("Folder", Resources.TreeFolder);
            _treeImages.Images.Add("Schema", Resources.TreeSchema);
            _treeImages.Images.Add("SchemaField", Resources.TreeField);
            _treeImages.Images.Add("SchemaFieldFolder", Resources.TreeDocument);
            _treeImages.Images.Add("SchemaIndex", Resources.TreeIndex);
            _treeImages.Images.Add("SchemaIndexFolder", Resources.TreeIndexFolder);
            _treeImages.Images.Add("Server", Resources.TreeServer);
            _treeImages.Images.Add("TreeNotLoaded", Resources.TreeNotLoaded);
            treeViewServerExplorer.ImageList = _treeImages;

            treeViewServerExplorer.Dock = DockStyle.Fill;
            splitContainerObjectExplorer.Dock = DockStyle.Fill;
            splitContainerMacros.Dock = DockStyle.Fill;
            tabControlBody.Dock = DockStyle.Fill;
            treeViewMacros.Dock = DockStyle.Fill;

            treeViewServerExplorer.NodeMouseClick += TreeViewServerExplorer_NodeMouseClick;

            tabControlBody.Click += TabControlBody_Click;
            tabControlBody.TabIndexChanged += TabControlBody_TabIndexChanged;

            treeViewMacros.ShowNodeToolTips = true;
            treeViewMacros.ItemDrag += TreeViewMacros_ItemDrag;

            Shown += FormStudio_Shown;
            FormClosing += FormStudio_FormClosing;

            tabControlBody.MouseUp += TabControlBody_MouseUp;

            splitContainerMacros.Panel2Collapsed = true;

            splitContainerObjectExplorer.SplitterDistance = Preferences.Instance.ObjectExplorerSplitterDistance;
            Width = Preferences.Instance.FormStudioWidth;
            Height = Preferences.Instance.FormStudioHeight;

            _toolbarSyncTimer.Tick += ToolbarSyncTimer_Tick;
            _toolbarSyncTimer.Interval = 250;
            _toolbarSyncTimer.Start();

            // Get the screen where the mouse cursor is located
            var screen = Screen.FromPoint(Cursor.Position);

            // Center the form on the screen
            StartPosition = FormStartPosition.Manual;
            Location = new Point(
                screen.Bounds.X + (screen.Bounds.Width - this.Width) / 2,
                screen.Bounds.Y + (screen.Bounds.Height - this.Height) / 2
            );

            ReloadRecentFileList();
        }

        #region Toolbar and Click Events.

        private void SyncToolbarAndMenuStates()
        {
            var tabFilePage = CurrentTabFilePage();

            toolStripStatusLabelServerName.Text = $"Server: {tabFilePage?.Client?.Address}:{tabFilePage?.Client?.Port}";
            toolStripStatusLabelProcessId.Text = "PID: " + tabFilePage?.Client?.ProcessId.ToString("N0") ?? string.Empty;

            bool isTabOpen = (tabFilePage != null);
            bool isTextSelected = (tabFilePage != null) && (tabFilePage?.Editor?.SelectionLength > 0);

            toolStripButtonCloseCurrentTab.Enabled = isTabOpen;
            toolStripButtonCopy.Enabled = isTextSelected;
            toolStripButtonCut.Enabled = isTextSelected;
            toolStripButtonFind.Enabled = isTabOpen;
            toolStripButtonPaste.Enabled = isTabOpen;
            toolStripButtonRedo.Enabled = isTabOpen;
            toolStripButtonReplace.Enabled = isTabOpen;
            toolStripButtonExpandAllRegions.Enabled = isTabOpen;
            toolStripButtonCollapseAllRegions.Enabled = isTabOpen;
            toolStripButtonExecuteScript.Enabled = isTabOpen && (tabFilePage?.IsScriptExecuting == false);
            toolStripButtonExplainPlan.Enabled = isTabOpen && (tabFilePage?.IsScriptExecuting == false);
            toolStripButtonStop.Enabled = isTabOpen && (tabFilePage?.IsScriptExecuting == true);

            toolStripButtonUndo.Enabled = isTabOpen;

            toolStripButtonDecreaseIndent.Enabled = isTextSelected;
            toolStripButtonIncreaseIndent.Enabled = isTextSelected;

            toolStripButtonSave.Enabled = isTabOpen;
            toolStripButtonSaveAll.Enabled = isTabOpen;
            toolStripButtonSnippets.Enabled = isTabOpen;
        }

        private void ToolbarSyncTimer_Tick(object? sender, EventArgs e)
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

        private void ToolStripButtonCollapseAllRegions_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.Editor.CollapseAllFolds();
        }

        private void ToolStripButtonExpandAllRegions_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.Editor.ExpandAllFolds();
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new FormSettings();
            if (form.ShowDialog() == DialogResult.OK)
            {
                foreach (var tabFilePage in tabControlBody.TabPages.OfType<CodeEditorTabPage>())
                {
                    FullyFeaturedCodeEditor.ApplyEditorSettings(tabFilePage.Editor);
                }
            }
        }

        private void ToolStripButtonNewFile_Click(object sender, EventArgs e)
        {
            try
            {
                var tabFilePage = CreateNewTabBasedOnLastSelectedNode();
                tabFilePage.Editor.Text = "set TraceWaitTimes false\r\n";
                tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ExecuteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.ExecuteCurrentScriptAsync(ExecuteType.Execute);
        }

        private void ExplainPlanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.ExecuteCurrentScriptAsync(ExecuteType.ExplainPlan);
        }

        private void ExplainOperationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.ExecuteCurrentScriptAsync(ExecuteType.ExplainOperations);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenTab();
        }

        private void SaveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            if (selection != null)
            {
                SaveTab(selection);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            if (selection != null)
            {
                SaveTabAs(selection);
            }
        }

        private void ToolStripButtonOpen_Click(object sender, EventArgs e)
        {
            OpenTab();
        }

        private void ToolStripButtonStop_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.ExecuteStopCommand();
        }

        private void ToolStripButtonExecuteScript_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.ExecuteCurrentScriptAsync(ExecuteType.Execute);
        }

        private void ToolStripButtonExplainPlan_Click(object sender, EventArgs e)
        {
            CurrentTabFilePage()?.ExecuteCurrentScriptAsync(ExecuteType.ExplainPlan);
        }

        private void ToolStripButtonCloseCurrentTab_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            CloseTab(selection);
        }

        private void ToolStripButtonSave_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            if (selection != null)
            {
                SaveTab(selection);
            }
        }

        bool SaveTab(CodeEditorTabPage tab)
        {
            if (tab.IsFileOpen == false)
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = $"{Api.KbConstants.FriendlyName} Script (*.kbs)|*.kbs|All files (*.*)|*.*";
                    sfd.FileName = tab.FilePath;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        return tab.Save(sfd.FileName);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return tab.Save();
        }

        bool SaveTabAs(CodeEditorTabPage tab)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = $"{KbConstants.FriendlyName} Script (*.kbs)|*.kbs|All files (*.*)|*.*";
                sfd.FileName = tab.FilePath;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    return tab.Save(sfd.FileName);
                }
                else
                {
                    return false;
                }
            }
        }

        private bool OpenTab()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = $"{KbConstants.FriendlyName} Script (*.kbs)|*.kbs|All files (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var alreadyOpenTab = FindTabByFileName(ofd.FileName);
                    if (alreadyOpenTab == null)
                    {
                        Preferences.Instance.AddRecentFile(ofd.FileName);
                        ReloadRecentFileList();
                        CreateNewTabBasedOnLastSelectedNode(Path.GetFileName(ofd.FileName)).OpenFile(ofd.FileName);
                    }
                    else
                    {
                        tabControlBody.SelectedTab = alreadyOpenTab;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #region Toolstrip Button Clicks.

        private void ToolStripButtonSaveAll_Click(object sender, EventArgs e)
        {
            foreach (var tabFilePage in tabControlBody.TabPages.OfType<CodeEditorTabPage>())
            {
                if (SaveTab(tabFilePage) == false)
                {
                    return;
                }
            }
        }

        private void ToolStripButtonFind_Click(object sender, EventArgs e)
        {
            ShowFind();
        }

        private void ToolStripButtonReplace_Click(object sender, EventArgs e)
        {
            ShowReplace();
        }

        private void ToolStripButtonRedo_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Redo();
        }

        private void ToolStripButtonUndo_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Undo();
        }

        private void ToolStripButtonCut_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Cut();
        }

        private void ToolStripButtonCopy_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Copy();
        }

        private void ToolStripButtonPaste_Click(object sender, EventArgs e)
        {
            var tabFilePage = CurrentTabFilePage();
            tabFilePage?.Editor.Paste();
        }

        private void ToolStripButtonIncreaseIndent_Click(object sender, EventArgs e)
        {
            IncreaseCurrentTabIndent();
        }

        private void ToolStripButtonDecreaseIndent_Click(object sender, EventArgs e)
        {
            DecreaseCurrentTabIndent();
        }

        private void toolStripButtonMacros_Click(object sender, EventArgs e)
        {
            splitContainerMacros.Panel2Collapsed = !splitContainerMacros.Panel2Collapsed;
        }

        private void ToolStripButtonProject_Click(object sender, EventArgs e)
        {
            splitContainerObjectExplorer.Panel1Collapsed = !splitContainerObjectExplorer.Panel1Collapsed;
        }

        private void ToolStripButtonOutput_Click(object sender, EventArgs e)
        {
            var tab = CurrentTabFilePage();
            if (tab != null)
            {
                tab.CollapseSplitter = !tab.CollapseSplitter;
            }
        }

        private void ToolStripButtonSnippets_Click(object sender, EventArgs e)
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

        public void IncreaseCurrentTabIndent()
        {
            var tabFilePage = CurrentTabFilePage();
            if (tabFilePage != null)
            {
                SendKeys.Send("{TAB}");
            }
        }

        public void DecreaseCurrentTabIndent()
        {
            SendKeys.Send("+({TAB})");
        }

        #endregion

        #region Find/Replace.

        private FormFindReplace? _findReplaceForm;
        private string _lastSearchText = string.Empty;
        private bool _lastSearchCaseSensitive = false;
        private string _lastReplaceText = string.Empty;

        public void ShowFind()
        {
            if (_findReplaceForm == null || _findReplaceForm.IsDisposed)
            {
                _findReplaceForm = new FormFindReplace(this, _lastSearchText, _lastReplaceText);
            }

            _findReplaceForm.Show(FormFindReplace.FindType.Find);
            _findReplaceForm.BringToFront();
        }

        public void ShowReplace()
        {
            if (_findReplaceForm == null || _findReplaceForm.IsDisposed)
            {
                _findReplaceForm = new FormFindReplace(this, _lastSearchText, _lastReplaceText);
            }

            _findReplaceForm.Show(FormFindReplace.FindType.Replace);
            _findReplaceForm.BringToFront();
        }

        public void FindNext(string searchText, bool caseSensitive)
        {
            _lastSearchText = searchText;
            _lastSearchCaseSensitive = caseSensitive;
            CurrentTabFilePage()?.FindNext(searchText, caseSensitive);
        }

        public void FindReplace(string searchText, string replaceWith, bool caseSensitive)
        {
            _lastSearchText = searchText;
            _lastReplaceText = replaceWith;
            _lastSearchCaseSensitive = caseSensitive;
            CurrentTabFilePage()?.FindReplace(searchText, replaceWith, caseSensitive);
        }

        public void FindReplaceAll(string searchText, string replaceWith, bool caseSensitive)
        {
            _lastSearchText = searchText;
            _lastReplaceText = replaceWith;
            _lastSearchCaseSensitive = caseSensitive;

            CurrentTabFilePage()?.FindReplaceAll(searchText, replaceWith, caseSensitive);
        }

        public void FindNext()
        {
            var info = CurrentTabFilePage();
            if (info != null)
            {
                if (string.IsNullOrEmpty(_lastSearchText))
                {
                    ShowFind();
                }
                else
                {
                    CurrentTabFilePage()?.FindNext(_lastSearchText, _lastSearchCaseSensitive);
                }
            }
        }

        #endregion

        #region Form events.

        private void FormStudio_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (CloseAllTabs() == false)
            {
                e.Cancel = true;
            }

            _serverExplorerManager.DisconnectAll();
        }

        private void FormStudio_Shown(object? sender, EventArgs e)
        {
            if (_firstShown == false)
            {
                return;
            }

            if (_firstShown)
            {
                try
                {
                    _firstShown = false;

                    var explorerConnection = Connect();

                    if (string.IsNullOrEmpty(_firstLoadFilename))
                    {
                        var tabFilePage = CreateNewTab(explorerConnection);
                        tabFilePage.Editor.Text = "set TraceWaitTimes false\r\n";
                        tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                        tabFilePage.IsSaved = true;
                    }
                    else
                    {
                        Preferences.Instance.AddRecentFile(_firstLoadFilename);
                        ReloadRecentFileList();
                        CreateNewTab(explorerConnection, Path.GetFileName(_firstLoadFilename)).OpenFile(_firstLoadFilename);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SyncToolbarAndMenuStates();
        }

        #endregion

        #region Server Explorer Treeview Shenanigans.

        private void TreeViewServerExplorer_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var popupMenu = new ContextMenuStrip();
            treeViewServerExplorer.SelectedNode = e.Node;

            popupMenu.ItemClicked += PopupMenu_ItemClicked;

            var node = e.Node as ServerExplorerNode;
            if (node == null)
            {
                throw new Exception("Invalid node type.");
            }

            popupMenu.Tag = e.Node as ServerExplorerNode;

            if (node.NodeType == Constants.ServerNodeType.Server)
            {
                popupMenu.Items.Add("Connect", FormUtility.TransparentImage(Resources.Workload));
                popupMenu.Items.Add("Disconnect", FormUtility.TransparentImage(Resources.Workload));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Refresh", FormUtility.TransparentImage(Resources.ToolFind));
            }
            else if (node.NodeType == Constants.ServerNodeType.Schema)
            {
                popupMenu.Items.Add("Create Index", FormUtility.TransparentImage(Resources.Asset));
                popupMenu.Items.Add("Select top n...", FormUtility.TransparentImage(Resources.Workload));
                popupMenu.Items.Add("Sample Schema", FormUtility.TransparentImage(Resources.Workload));
                popupMenu.Items.Add("Analyze Schema", FormUtility.TransparentImage(Resources.Workload));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Drop Schema", FormUtility.TransparentImage(Resources.Asset));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Refresh", FormUtility.TransparentImage(Resources.ToolFind));
            }
            else if (node.NodeType == Constants.ServerNodeType.SchemaIndexFolder)
            {
                popupMenu.Items.Add("Create Index", FormUtility.TransparentImage(Resources.Asset));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Refresh", FormUtility.TransparentImage(Resources.ToolFind));
            }
            else if (node.NodeType == Constants.ServerNodeType.SchemaFieldFolder)
            {
                popupMenu.Items.Add("Refresh", FormUtility.TransparentImage(Resources.ToolFind));
            }
            else if (node.NodeType == Constants.ServerNodeType.SchemaIndex)
            {
                popupMenu.Items.Add("Analyze Index", FormUtility.TransparentImage(Resources.Asset));
                popupMenu.Items.Add("Script Index", FormUtility.TransparentImage(Resources.Asset));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Rebuild Index", FormUtility.TransparentImage(Resources.Asset));
                popupMenu.Items.Add("Drop Index", FormUtility.TransparentImage(Resources.Asset));
            }

            popupMenu.Show(treeViewServerExplorer, e.Location);
        }

        private void PopupMenu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            try
            {
                var menuStrip = (sender as ContextMenuStrip).EnsureNotNull();
                var node = (menuStrip.Tag as ServerExplorerNode).EnsureNotNull();
                var schema = ServerExplorerManager.GetSchemaNodeFor(node)?.Schema;

                menuStrip.Hide();

                if (e.ClickedItem?.Text?.Is("Connect") == true)
                {
                    Connect();
                }
                else if (e.ClickedItem?.Text?.Is("Disconnect") == true)
                {
                    if (node.NodeType == Constants.ServerNodeType.Server)
                    {
                        node.ExplorerConnection?.Disconnect();
                        node.Remove();

                        if (_serverExplorerManager.LastSelectedNode == node)
                        {
                            _serverExplorerManager.LastSelectedNode = null;
                        }
                    }
                }
                else if (e.ClickedItem?.Text?.Is("Refresh") == true)
                {
                    if (node.NodeType == Constants.ServerNodeType.Server)
                    {
                        var serverNode = ServerExplorerManager.GetServerNodeFor(node);
                        if (serverNode != null && serverNode.ExplorerConnection != null)
                        {
                            var rootSchema = ServerExplorerManager.GetFirstChildNodeOfType(node, Constants.ServerNodeType.Schema);
                            if (rootSchema != null)
                            {
                                rootSchema.Nodes.Clear();
                                serverNode.ExplorerConnection.LazySchemaCache.Refresh(string.Empty);
                            }
                        }
                    }
                    /*
                    else if (node.NodeType == Constants.ServerNodeType.Schema && schema?.Path != null)
                    {
                        if (schema?.Id == EngineConstants.RootSchemaGUID)
                        {
                            node.Nodes.Clear();
                        }
                        else
                        {
                            node.Remove();
                        }

                        LazySchemaCache.Refresh(schema?.Path);
                    }
                    else if (node.NodeType == Constants.ServerNodeType.SchemaFieldFolder || node.NodeType == Constants.ServerNodeType.SchemaIndexFolder)
                    {
                        //Fake field/index refresh, just refresh the parent node.

                        var parentNode = (ServerExplorerNode)node.Parent;
                        if (parentNode.NodeType == Constants.ServerNodeType.Schema && parentSchema?.Path != null)
                        {
                            if (parentSchema?.Id == EngineConstants.RootSchemaGUID)
                            {
                                parentNode.Nodes.Clear();
                            }
                            else
                            {
                                parentNode.Remove();
                            }

                            LazySchemaCache.Refresh(parentSchema?.Path);
                        }
                    }
                    */
                }
                else if (e.ClickedItem?.Text?.Is("Select top n...") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);
                    tabFilePage.Editor.Text = $"SELECT TOP 100\r\n\t*\r\nFROM\r\n\t{schema.Path}\r\n";
                    tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                    tabFilePage.ExecuteCurrentScriptAsync(ExecuteType.Execute);
                }
                else if (e.ClickedItem?.Text?.Is("Drop Schema") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);
                    tabFilePage.Editor.Text = $"DROP SCHEMA {schema.Path}\r\n";
                    tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                }
                else if (e.ClickedItem?.Text?.Is("Analyze Schema") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);
                    tabFilePage.Editor.Text = $"ANALYZE SCHEMA {schema.Path} --WITH (IncludePhysicalPages = true)\r\n";
                    tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                    tabFilePage.ExecuteCurrentScriptAsync(ExecuteType.Execute);
                }
                else if (e.ClickedItem?.Text?.Is("Sample Schema") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);
                    tabFilePage.Editor.Text = $"SAMPLE {schema.Path} SIZE 100\r\n";
                    tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                    tabFilePage.TabSplitContainer.SplitterDistance = 60;
                    tabFilePage.ExecuteCurrentScriptAsync(ExecuteType.Execute);
                }
                else if (e.ClickedItem?.Text?.Is("Drop Index") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);
                    tabFilePage.Editor.Text = $"DROP INDEX {node.Text} ON {schema.Path}\r\n";
                    tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                    tabFilePage.TabSplitContainer.SplitterDistance = 60;
                }
                else if (e.ClickedItem?.Text?.Is("Analyze Index") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);
                    tabFilePage.Editor.Text = $"ANALYZE INDEX {node.Text} ON {schema.Path}\r\n";
                    tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                    tabFilePage.TabSplitContainer.SplitterDistance = 60;
                    tabFilePage.ExecuteCurrentScriptAsync(ExecuteType.Execute);
                }
                else if (e.ClickedItem?.Text?.Is("Rebuild Index") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);
                    if (tabFilePage.Client != null)
                    {
                        var result = tabFilePage.Client.Schema.Indexes.Get(schema.Path, node.Text);
                        if (result != null && result.Index != null)
                        {
                            var text = new StringBuilder("REBUILD ");
                            text.Append(result.Index.IsUnique ? "UNIQUEKEY" : "INDEX");
                            text.Append($" {result.Index.Name} ON {schema.Path}");
                            text.AppendLine($" WITH (PARTITIONS={result.Index.Partitions})");

                            tabFilePage.Editor.Text = text.ToString();
                            tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                            tabFilePage.TabSplitContainer.SplitterDistance = 60;
                        }
                    }
                }
                else if (e.ClickedItem?.Text?.Is("Script Index") == true && schema != null)
                {
                    var tabFilePage = CreateNewTabBasedOn(node);

                    if (tabFilePage.Client != null)
                    {
                        var result = tabFilePage.Client.Schema.Indexes.Get(schema.Path, node.Text);
                        if (result != null && result.Index != null)
                        {
                            var text = new StringBuilder("CREATE ");
                            text.Append(result.Index.IsUnique ? "UNIQUEKEY" : "INDEX");
                            text.Append($" {result.Index.Name}");
                            text.AppendLine("(");
                            foreach (var attribute in result.Index.Attributes)
                            {
                                text.AppendLine($"    {attribute.Field},");
                            }
                            text.Length -= 3;//Remove trialing ",\r\n"
                            text.Append($"\r\n) ON {schema.Path}");
                            text.AppendLine($" WITH (PARTITIONS={result.Index.Partitions})");

                            tabFilePage.Editor.Text = text.ToString();
                            tabFilePage.Editor.SelectionStart = tabFilePage.Editor.Text.Length;
                            tabFilePage.TabSplitContainer.SplitterDistance = 60;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Macros Treeview.

        private void PopulateMacros(KbClient client)
        {
            GlobalState.AutoCompleteFunctions.Clear();

            var functionsNode = treeViewMacros.Nodes.Find("Functions", false).FirstOrDefault();
            if (functionsNode == null)
            {
                functionsNode = treeViewMacros.Nodes.Add("Functions", "Functions");
            }

            functionsNode.Nodes.Clear();

            #region System Functions.

            var systemFunctionsNode = functionsNode.Nodes.Add("System");
            var systemFunctions = client.Query.Fetch<KbFunctionDescriptor>("EXEC ShowSystemFunctions").OrderBy(o => o.Name);
            foreach (var systemFunction in systemFunctions)
            {
                var node = new FunctionTreeNode(systemFunction);
                var autoCompleteFunctionParameters = new List<AutoCompleteFunctionParameter>();

                if (systemFunction.Parameters != null)
                {
                    var parameters = systemFunction.Parameters.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => ParseParameterParts(o.Trim()));
                    foreach (var parameter in parameters)
                    {
                        autoCompleteFunctionParameters.Add(new AutoCompleteFunctionParameter(parameter.DataType, parameter.Name));
                        node.Nodes.Add($"{parameter.Value}");
                    }
                }
                systemFunctionsNode.Nodes.Add(node);

                var autoCompleteFunction = new AutoCompleteFunction(FunctionType.System, systemFunction.Name ?? string.Empty,
                    systemFunction.ReturnType ?? string.Empty, systemFunction.Description ?? string.Empty, autoCompleteFunctionParameters);
                GlobalState.AutoCompleteFunctions.Add(autoCompleteFunction);
            }

            #endregion

            #region Scalar Functions.

            var scalarFunctionsNode = functionsNode.Nodes.Add("Scalar");
            var scalarFunctions = client.Query.Fetch<KbFunctionDescriptor>("EXEC ShowScalarFunctions").OrderBy(o => o.Name);
            foreach (var scalarFunction in scalarFunctions)
            {
                var node = new FunctionTreeNode(scalarFunction);
                var autoCompleteFunctionParameters = new List<AutoCompleteFunctionParameter>();

                if (scalarFunction.Parameters != null)
                {
                    var parameters = scalarFunction.Parameters.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => ParseParameterParts(o.Trim()));
                    foreach (var parameter in parameters)
                    {
                        autoCompleteFunctionParameters.Add(new AutoCompleteFunctionParameter(parameter.DataType, parameter.Name));
                        node.Nodes.Add($"{parameter.Value}");
                    }
                }
                scalarFunctionsNode.Nodes.Add(node);

                var autoCompleteFunction = new AutoCompleteFunction(FunctionType.Scalar, scalarFunction.Name ?? string.Empty,
                    scalarFunction.ReturnType ?? string.Empty, scalarFunction.Description ?? string.Empty, autoCompleteFunctionParameters);
                GlobalState.AutoCompleteFunctions.Add(autoCompleteFunction);
            }

            #endregion

            #region Aggregate Functions.

            var aggregateFunctionsNode = functionsNode.Nodes.Add("Aggregate");
            var aggregateFunctions = client.Query.Fetch<KbFunctionDescriptor>("EXEC ShowAggregateFunctions").OrderBy(o => o.Name);
            foreach (var aggregateFunction in aggregateFunctions)
            {
                var node = new FunctionTreeNode(aggregateFunction);
                var autoCompleteFunctionParameters = new List<AutoCompleteFunctionParameter>();

                if (aggregateFunction.Parameters != null)
                {
                    var parameters = aggregateFunction.Parameters.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => ParseParameterParts(o.Trim()));
                    foreach (var parameter in parameters)
                    {
                        autoCompleteFunctionParameters.Add(new AutoCompleteFunctionParameter(parameter.DataType, parameter.Name));
                        node.Nodes.Add($"{parameter.Value}");
                    }
                }
                aggregateFunctionsNode.Nodes.Add(node);

                var autoCompleteFunction = new AutoCompleteFunction(FunctionType.Aggregate, aggregateFunction.Name ?? string.Empty,
                    aggregateFunction.ReturnType ?? string.Empty, aggregateFunction.Description ?? string.Empty, autoCompleteFunctionParameters);
                GlobalState.AutoCompleteFunctions.Add(autoCompleteFunction);
            }

            #endregion

            functionsNode.Expand();

            (string Value, string DataType, string Name, string DefaultValue) ParseParameterParts(string parameter)
            {
                (string Value, string DataType, string Name, string DefaultValue) result = new()
                {
                    Value = parameter
                };

                var paramParts = parameter.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList();
                if (paramParts.Count > 0)
                {
                    result.DataType = paramParts[0];
                }
                if (paramParts.Count > 1)
                {
                    result.Name = paramParts[1];
                }
                if (paramParts.Count > 2)
                {
                    string defaultValue = string.Join(' ', paramParts.Skip(2));
                    if (defaultValue.StartsWith('='))
                    {
                        result.DefaultValue = defaultValue.Trim([' ', '=', '\t']);
                    }
                }

                return result;
            }
        }

        #endregion

        #region Body Tab Magic.

        private void TabControlBody_TabIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                if (sender is TabControl tabControl && tabControl.SelectedTab != null)
                {
                    ((CodeEditorTabPage)tabControl.SelectedTab).Editor.Focus();
                }
            }
            catch
            {
            }
        }

        private void TabControlBody_Click(object? sender, EventArgs e)
        {
            try
            {
                if (sender is TabControl tabControl && tabControl.SelectedTab != null)
                {
                    ((CodeEditorTabPage)tabControl.SelectedTab).Editor.Focus();
                }
            }
            catch
            {
            }
        }

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
                popupMenu.ItemClicked += PopupMenu_tabControlScripts_MouseUp_ItemClicked;

                popupMenu.Tag = clickedTab;

                popupMenu.Items.Add("Close", FormUtility.TransparentImage(Resources.ToolCloseFile));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Close All but This", FormUtility.TransparentImage(Resources.ToolCloseFile));
                popupMenu.Items.Add("Close All", FormUtility.TransparentImage(Resources.ToolCloseFile));
                popupMenu.Items.Add("-");
                popupMenu.Items.Add("Find in Server Explorer", FormUtility.TransparentImage(Resources.ToolFind));
                popupMenu.Items.Add("Open Containing Folder", FormUtility.TransparentImage(Resources.ToolOpenFile));
                popupMenu.Show(tabControlBody, e.Location);
            }
        }

        private void PopupMenu_tabControlScripts_MouseUp_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
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

            CodeEditorTabPage? clickedTab = contextMenu.Tag as CodeEditorTabPage;
            if (clickedTab == null)
            {
                return;
            }

            if (clickedItem.Text?.Is("Close") == true)
            {
                CloseTab(clickedTab);
            }
            else if (clickedItem.Text?.Is("Open Containing Folder") == true)
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
            else if (clickedItem.Text?.Is("Close All but This") == true)
            {
                var tabsToClose = new List<CodeEditorTabPage>();

                //Minimize the number of "SelectedIndexChanged" events that get fired.
                //We get a big ol' thread exception when we don't. Looks like an internal control exception.
                tabControlBody.SelectedTab = clickedTab;
                Application.DoEvents(); //Make sure the message pump can actually select the tab before we start closing.

                foreach (var tabFilePage in tabControlBody.TabPages.OfType<CodeEditorTabPage>())
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
            else if (clickedItem.Text?.Is("Close All") == true)
            {
                CloseAllTabs();
            }

            //UpdateToolbarButtonStates();
        }

        private CodeEditorTabPage? GetClickedTab(Point mouseLocation)
        {
            for (int i = 0; i < tabControlBody.TabCount; i++)
            {
                Rectangle r = tabControlBody.GetTabRect(i);
                if (r.Contains(mouseLocation))
                {
                    return (CodeEditorTabPage)tabControlBody.TabPages[i];
                }
            }
            return null;
        }

        private CodeEditorTabPage? FindTabByFileName(string filePath)
        {
            foreach (var tab in tabControlBody.TabPages.OfType<CodeEditorTabPage>())
            {
                if (tab.FilePath.ToLower() == filePath.ToLower())
                {
                    return tab;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new empty code tab based on the latest selected node in
        ///     the server explorer, will create a new client from that manager.
        /// </summary>
        private CodeEditorTabPage CreateNewFileEmptyTab()
        {
            var serverNode = _serverExplorerManager.GetServerNodeForLastSelectedNode();
            return CreateNewTab(serverNode?.ExplorerConnection, FormUtility.GetNextNewFileName());
        }

        /// <summary>
        /// Creates a new empty code tab based on the the server connection associated
        ///     with the given node, will create a new client from that manager.
        /// </summary>
        /// <param name="basedOnNode"></param>
        /// <returns></returns>
        private CodeEditorTabPage CreateNewTabBasedOn(ServerExplorerNode basedOnNode)
        {
            var serverNode = ServerExplorerManager.GetServerNodeFor(basedOnNode);
            return CreateNewTab(serverNode?.ExplorerConnection, FormUtility.GetNextNewFileName());
        }

        /// <summary>
        /// Creates a new empty code tab based on the latest selected node in the
        ///     server explorer, will create a new client from that manager.
        /// </summary>
        private CodeEditorTabPage CreateNewTabBasedOnLastSelectedNode(string tabText = "")
        {
            var serverNode = _serverExplorerManager.GetServerNodeForLastSelectedNode();
            return CreateNewTab(serverNode?.ExplorerConnection, tabText);
        }

        /// <summary>
        /// Creates a new tab using the specified server explorer manager, will create a new client from that manager.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tabText"></param>
        /// <returns></returns>
        private CodeEditorTabPage CreateNewTab(ServerExplorerConnection? explorerConnection, string tabText = "")
        {
            var client = explorerConnection?.CreateNewConnection();

            if (string.IsNullOrWhiteSpace(tabText))
            {
                tabText = FormUtility.GetNextNewFileName();
            }

            var codeTabPage = new CodeEditorTabPage(this, tabControlBody, client, explorerConnection, tabText);

            tabControlBody.TabPages.Add(codeTabPage);
            tabControlBody.SelectedTab = codeTabPage;
            codeTabPage.Editor.Focus();

            return codeTabPage;
        }

        /// <summary>
        /// Removes a tab, saved or not - no prompting.
        /// </summary>
        /// <param name="tab"></param>
        private void RemoveTab(CodeEditorTabPage? tab)
        {
            if (tab != null)
            {
                tabControlBody.TabPages.Remove(tab);
                tab.Dispose();
            }
            SyncToolbarAndMenuStates();
        }

        bool CloseAllTabs()
        {
            //Minimize the number of "SelectedIndexChanged" events that get fired.
            //We get a big ol' thread exception when we don't do  Looks like an internal control exception.
            tabControlBody.SelectedIndex = 0;
            Application.DoEvents(); //Make sure the message pump can actually select the tab before we start closing.

            tabControlBody.SuspendLayout();

            bool result = true;
            while (tabControlBody.TabPages.Count != 0)
            {
                if (!CloseTab(tabControlBody.TabPages[tabControlBody.TabPages.Count - 1] as CodeEditorTabPage))
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
        /// User friendly tab close.
        /// </summary>
        /// <param name="tab"></param>
        private bool CloseTab(CodeEditorTabPage? tab)
        {
            if (tab != null)
            {
                if (tab.IsSaved == false)
                {
                    var messageBoxResult = MessageBox.Show(
                        "Save \"" + tab.Text.Trim(['*']) + "\" before closing?", "Save File?",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (messageBoxResult == DialogResult.Yes)
                    {
                        if (SaveTab(tab) == false)
                        {
                            return false;
                        }
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

        public CodeEditorTabPage? CurrentTabFilePage()
        {
            if (tabControlBody.InvokeRequired)
            {
                return tabControlBody.Invoke(new Func<CodeEditorTabPage?>(CurrentTabFilePage));
            }
            else
            {
                return tabControlBody.SelectedTab as CodeEditorTabPage;
            }
        }

        #endregion

        #region Form Menu.

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            if (selection == null)
            {
                return;
            }
            SaveTab(selection);
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selection = CurrentTabFilePage();
            CloseTab(selection);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new FormAbout();
            form.ShowDialog();
        }

        private void SaveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var tabFilePage in tabControlBody.TabPages.OfType<CodeEditorTabPage>())
            {
                if (SaveTab(tabFilePage) == false)
                {
                    break;
                }
            }
        }

        private void ConnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseAllTabs();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region Drap Drop.

        private void FormStudio_DragDrop(object sender, DragEventArgs e)
        {
            var files = e.Data?.GetData(DataFormats.FileDrop, false) as string[];
            if (files != null)
            {
                foreach (var file in files)
                {
                    OpenFileOrSelectExisting(file);
                }
            }
        }

        private void FormStudio_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private static string GetDraggedItemPath(ServerExplorerNode node)
        {
            if (node.NodeType == Constants.ServerNodeType.Schema)
            {
                return node.Schema.EnsureNotNull().Path;
            }
            else if (node.NodeType == Constants.ServerNodeType.SchemaField
                || node.NodeType == Constants.ServerNodeType.SchemaIndex)
            {
                return node.Text;
            }

            return string.Empty;
        }

        /// <summary>
        /// Drag an item from the macros tree view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeViewMacros_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is FunctionTreeNode functionNode)
            {
                treeViewMacros.DoDragDrop($"{functionNode.Function.Name}({functionNode.Function.Parameters})", DragDropEffects.All);
            }
        }

        /// <summary>
        /// Drag an item from the server explorer tree view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeViewServerExplorer_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node)
            {
                string text = GetDraggedItemPath((ServerExplorerNode)node);

                if (string.IsNullOrEmpty(text) == false)
                {
                    treeViewServerExplorer.DoDragDrop(text, DragDropEffects.Copy);
                }
            }
        }

        #endregion

        private void ReloadRecentFileList()
        {
            recentFilesToolStripMenuItem.DropDownItems.Clear();
            foreach (var recentFiles in Preferences.Instance.RecentFiles)
            {
                recentFilesToolStripMenuItem.DropDownItems.Add(recentFiles).Click += (object? sender, EventArgs e) =>
                {
                    if (sender is ToolStripItem menuItem)
                    {
                        var fileName = menuItem.Text;
                        if (string.IsNullOrEmpty(fileName) == false)
                        {
                            if (File.Exists(fileName))
                            {
                                CreateNewTabBasedOnLastSelectedNode(Path.GetFileName(fileName)).OpenFile(fileName);
                            }
                            else
                            {
                                Preferences.Instance.RemoveRecentFile(fileName);
                            }
                        }
                        else
                        {
                            Preferences.Instance.RemoveRecentFile(recentFilesToolStripMenuItem.Text.EnsureNotNull());
                        }
                    }
                };
            }
            recentFilesToolStripMenuItem.Visible = recentFilesToolStripMenuItem.HasDropDownItems;
        }

        private ServerExplorerConnection? Connect()
        {
            try
            {
                using var form = new FormConnect();
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var existingExplorerConnection = _serverExplorerManager.FindServerNode(form.ServerAddress, form.ServerPort, form.Username);
                    if (existingExplorerConnection != null && existingExplorerConnection.ExplorerConnection != null)
                    {
                        //Recycle already connected server explorer connections.
                        treeViewServerExplorer.SelectedNode = existingExplorerConnection;
                        return existingExplorerConnection.ExplorerConnection;
                    }

                    var explorerConnection = new ServerExplorerConnection(this,
                        _serverExplorerManager, form.ServerAddress, form.ServerPort, form.Username, form.PasswordHash);

                    foreach (TreeNode node in treeViewServerExplorer.Nodes)
                    {
                        node.Expand();
                    }

                    if (explorerConnection.Client != null)
                    {
                        Threading.StartThread(() =>
                        {
                            //using var macrosConnection = explorerConnection.CreateNewConnection();
                            Invoke(() =>
                            {
                                PopulateMacros(explorerConnection.Client);
                            });
                        });
                    }

                    return explorerConnection;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, KbConstants.FriendlyName);
            }

            return null;
        }

        public void OpenFileOrSelectExisting(string fileName)
        {
            var alreadyOpenTab = FindTabByFileName(fileName);
            if (alreadyOpenTab == null)
            {
                Preferences.Instance.AddRecentFile(fileName);
                ReloadRecentFileList();
                CreateNewTabBasedOnLastSelectedNode(Path.GetFileName(fileName)).OpenFile(fileName);
            }
            else
            {
                tabControlBody.SelectedTab = alreadyOpenTab;
            }
        }

        private void FormStudio_ResizeEnd(object sender, EventArgs e)
        {
            Preferences.Instance.FormStudioWidth = Width;
            Preferences.Instance.FormStudioHeight = Height;
        }

        private void SplitContainerProject_SplitterMoved(object sender, SplitterEventArgs e)
        {
            Preferences.Instance.ObjectExplorerSplitterDistance = splitContainerObjectExplorer.SplitterDistance;
        }
    }
}
