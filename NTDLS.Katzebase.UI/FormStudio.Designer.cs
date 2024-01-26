namespace NTDLS.Katzebase.UI
{
    partial class FormStudio
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormStudio));
            splitContainerObjectExplorer = new SplitContainer();
            treeViewProject = new TreeView();
            splitContainerMacros = new SplitContainer();
            tabControlBody = new TabControl();
            treeViewMacros = new TreeView();
            statusStripDocument = new StatusStrip();
            toolStripStatusLabelServerName = new ToolStripStatusLabel();
            toolStripStatusLabelProcessId = new ToolStripStatusLabel();
            toolStripButtons = new ToolStrip();
            toolStripButtonNewFile = new ToolStripButton();
            toolStripButtonOpen = new ToolStripButton();
            toolStripButtonSave = new ToolStripButton();
            toolStripButtonSaveAll = new ToolStripButton();
            toolStripButtonCloseCurrentTab = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripButtonExplainPlan = new ToolStripButton();
            toolStripButtonExecuteScript = new ToolStripButton();
            toolStripButtonStop = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            toolStripButtonFind = new ToolStripButton();
            toolStripButtonReplace = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            toolStripButtonUndo = new ToolStripButton();
            toolStripButtonRedo = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            toolStripButtonCut = new ToolStripButton();
            toolStripButtonCopy = new ToolStripButton();
            toolStripButtonPaste = new ToolStripButton();
            toolStripSeparator5 = new ToolStripSeparator();
            toolStripButtonDecreaseIndent = new ToolStripButton();
            toolStripButtonIncreaseIndent = new ToolStripButton();
            toolStripSeparator6 = new ToolStripSeparator();
            toolStripButtonProject = new ToolStripButton();
            toolStripButtonOutput = new ToolStripButton();
            toolStripButtonMacros = new ToolStripButton();
            toolStripSeparator7 = new ToolStripSeparator();
            toolStripButtonSnippets = new ToolStripButton();
            menuStripMain = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            connectToolStripMenuItem = new ToolStripMenuItem();
            closeProjectToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            openToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem1 = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            saveAllToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            documentToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            closeToolStripMenuItem = new ToolStripMenuItem();
            closeAllToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)splitContainerObjectExplorer).BeginInit();
            splitContainerObjectExplorer.Panel1.SuspendLayout();
            splitContainerObjectExplorer.Panel2.SuspendLayout();
            splitContainerObjectExplorer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMacros).BeginInit();
            splitContainerMacros.Panel1.SuspendLayout();
            splitContainerMacros.Panel2.SuspendLayout();
            splitContainerMacros.SuspendLayout();
            statusStripDocument.SuspendLayout();
            toolStripButtons.SuspendLayout();
            menuStripMain.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerObjectExplorer
            // 
            splitContainerObjectExplorer.Dock = DockStyle.Fill;
            splitContainerObjectExplorer.FixedPanel = FixedPanel.Panel1;
            splitContainerObjectExplorer.Location = new Point(0, 49);
            splitContainerObjectExplorer.Name = "splitContainerObjectExplorer";
            // 
            // splitContainerObjectExplorer.Panel1
            // 
            splitContainerObjectExplorer.Panel1.Controls.Add(treeViewProject);
            // 
            // splitContainerObjectExplorer.Panel2
            // 
            splitContainerObjectExplorer.Panel2.Controls.Add(splitContainerMacros);
            splitContainerObjectExplorer.Panel2.Controls.Add(statusStripDocument);
            splitContainerObjectExplorer.Size = new Size(1184, 712);
            splitContainerObjectExplorer.SplitterDistance = 320;
            splitContainerObjectExplorer.TabIndex = 0;
            splitContainerObjectExplorer.SplitterMoved += splitContainerProject_SplitterMoved;
            // 
            // treeViewProject
            // 
            treeViewProject.Location = new Point(14, 35);
            treeViewProject.Name = "treeViewProject";
            treeViewProject.Size = new Size(256, 292);
            treeViewProject.TabIndex = 0;
            treeViewProject.ItemDrag += treeViewProject_ItemDrag;
            treeViewProject.DragDrop += FormStudio_DragDrop;
            // 
            // splitContainerMacros
            // 
            splitContainerMacros.FixedPanel = FixedPanel.Panel2;
            splitContainerMacros.Location = new Point(44, 35);
            splitContainerMacros.Name = "splitContainerMacros";
            // 
            // splitContainerMacros.Panel1
            // 
            splitContainerMacros.Panel1.Controls.Add(tabControlBody);
            // 
            // splitContainerMacros.Panel2
            // 
            splitContainerMacros.Panel2.Controls.Add(treeViewMacros);
            splitContainerMacros.Size = new Size(529, 236);
            splitContainerMacros.SplitterDistance = 261;
            splitContainerMacros.TabIndex = 1;
            // 
            // tabControlBody
            // 
            tabControlBody.Location = new Point(24, 22);
            tabControlBody.Name = "tabControlBody";
            tabControlBody.SelectedIndex = 0;
            tabControlBody.Size = new Size(216, 189);
            tabControlBody.TabIndex = 0;
            // 
            // treeViewMacros
            // 
            treeViewMacros.Location = new Point(13, 22);
            treeViewMacros.Name = "treeViewMacros";
            treeViewMacros.Size = new Size(168, 189);
            treeViewMacros.TabIndex = 0;
            // 
            // statusStripDocument
            // 
            statusStripDocument.GripStyle = ToolStripGripStyle.Visible;
            statusStripDocument.Items.AddRange(new ToolStripItem[] { toolStripStatusLabelServerName, toolStripStatusLabelProcessId });
            statusStripDocument.Location = new Point(0, 690);
            statusStripDocument.Name = "statusStripDocument";
            statusStripDocument.Size = new Size(860, 22);
            statusStripDocument.TabIndex = 2;
            statusStripDocument.Text = "statusStripDocument";
            // 
            // toolStripStatusLabelServerName
            // 
            toolStripStatusLabelServerName.Name = "toolStripStatusLabelServerName";
            toolStripStatusLabelServerName.Size = new Size(39, 17);
            toolStripStatusLabelServerName.Text = "Server";
            // 
            // toolStripStatusLabelProcessId
            // 
            toolStripStatusLabelProcessId.Name = "toolStripStatusLabelProcessId";
            toolStripStatusLabelProcessId.Size = new Size(47, 17);
            toolStripStatusLabelProcessId.Text = "Process";
            // 
            // toolStripButtons
            // 
            toolStripButtons.Items.AddRange(new ToolStripItem[] { toolStripButtonNewFile, toolStripButtonOpen, toolStripButtonSave, toolStripButtonSaveAll, toolStripButtonCloseCurrentTab, toolStripSeparator1, toolStripButtonExplainPlan, toolStripButtonExecuteScript, toolStripButtonStop, toolStripSeparator2, toolStripButtonFind, toolStripButtonReplace, toolStripSeparator3, toolStripButtonUndo, toolStripButtonRedo, toolStripSeparator4, toolStripButtonCut, toolStripButtonCopy, toolStripButtonPaste, toolStripSeparator5, toolStripButtonDecreaseIndent, toolStripButtonIncreaseIndent, toolStripSeparator6, toolStripButtonProject, toolStripButtonOutput, toolStripButtonMacros, toolStripSeparator7, toolStripButtonSnippets });
            toolStripButtons.Location = new Point(0, 24);
            toolStripButtons.Name = "toolStripButtons";
            toolStripButtons.Size = new Size(1184, 25);
            toolStripButtons.TabIndex = 1;
            toolStripButtons.Text = "toolStrip1";
            // 
            // toolStripButtonNewFile
            // 
            toolStripButtonNewFile.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonNewFile.Image = Properties.Resources.ToolNewFile;
            toolStripButtonNewFile.ImageTransparentColor = Color.Magenta;
            toolStripButtonNewFile.Name = "toolStripButtonNewFile";
            toolStripButtonNewFile.Size = new Size(23, 22);
            toolStripButtonNewFile.Text = "New File";
            toolStripButtonNewFile.Click += toolStripButtonNewFile_Click;
            // 
            // toolStripButtonOpen
            // 
            toolStripButtonOpen.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonOpen.Image = Properties.Resources.ToolOpenFile;
            toolStripButtonOpen.ImageTransparentColor = Color.Magenta;
            toolStripButtonOpen.Name = "toolStripButtonOpen";
            toolStripButtonOpen.Size = new Size(23, 22);
            toolStripButtonOpen.Text = "Open";
            toolStripButtonOpen.Click += toolStripButtonOpen_Click;
            // 
            // toolStripButtonSave
            // 
            toolStripButtonSave.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonSave.Image = Properties.Resources.ToolSave;
            toolStripButtonSave.ImageTransparentColor = Color.Magenta;
            toolStripButtonSave.Name = "toolStripButtonSave";
            toolStripButtonSave.Size = new Size(23, 22);
            toolStripButtonSave.Text = "Save";
            toolStripButtonSave.Click += toolStripButtonSave_Click;
            // 
            // toolStripButtonSaveAll
            // 
            toolStripButtonSaveAll.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonSaveAll.Image = Properties.Resources.ToolSaveAll;
            toolStripButtonSaveAll.ImageTransparentColor = Color.Magenta;
            toolStripButtonSaveAll.Name = "toolStripButtonSaveAll";
            toolStripButtonSaveAll.Size = new Size(23, 22);
            toolStripButtonSaveAll.Text = "Save All";
            toolStripButtonSaveAll.Click += toolStripButtonSaveAll_Click;
            // 
            // toolStripButtonCloseCurrentTab
            // 
            toolStripButtonCloseCurrentTab.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonCloseCurrentTab.Image = Properties.Resources.ToolCloseFile;
            toolStripButtonCloseCurrentTab.ImageTransparentColor = Color.Magenta;
            toolStripButtonCloseCurrentTab.Name = "toolStripButtonCloseCurrentTab";
            toolStripButtonCloseCurrentTab.Size = new Size(23, 22);
            toolStripButtonCloseCurrentTab.Text = "Close Current Tab";
            toolStripButtonCloseCurrentTab.Click += toolStripButtonCloseCurrentTab_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // toolStripButtonExplainPlan
            // 
            toolStripButtonExplainPlan.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonExplainPlan.Image = Properties.Resources.ToolRunOne;
            toolStripButtonExplainPlan.ImageTransparentColor = Color.Magenta;
            toolStripButtonExplainPlan.Name = "toolStripButtonExplainPlan";
            toolStripButtonExplainPlan.Size = new Size(23, 22);
            toolStripButtonExplainPlan.Text = "Explain Plan";
            toolStripButtonExplainPlan.Click += toolStripButtonExplainPlan_Click;
            // 
            // toolStripButtonExecuteScript
            // 
            toolStripButtonExecuteScript.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonExecuteScript.Image = Properties.Resources.ToolRun;
            toolStripButtonExecuteScript.ImageTransparentColor = Color.Magenta;
            toolStripButtonExecuteScript.Name = "toolStripButtonExecuteScript";
            toolStripButtonExecuteScript.Size = new Size(23, 22);
            toolStripButtonExecuteScript.Text = "Execute Script";
            toolStripButtonExecuteScript.Click += toolStripButtonExecuteScript_Click;
            // 
            // toolStripButtonStop
            // 
            toolStripButtonStop.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonStop.Image = Properties.Resources.ToolStop;
            toolStripButtonStop.ImageTransparentColor = Color.Magenta;
            toolStripButtonStop.Name = "toolStripButtonStop";
            toolStripButtonStop.Size = new Size(23, 22);
            toolStripButtonStop.Text = "Stop";
            toolStripButtonStop.Click += toolStripButtonStop_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // toolStripButtonFind
            // 
            toolStripButtonFind.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonFind.Image = Properties.Resources.ToolFind;
            toolStripButtonFind.ImageTransparentColor = Color.Magenta;
            toolStripButtonFind.Name = "toolStripButtonFind";
            toolStripButtonFind.Size = new Size(23, 22);
            toolStripButtonFind.Text = "Find";
            toolStripButtonFind.Click += toolStripButtonFind_Click;
            // 
            // toolStripButtonReplace
            // 
            toolStripButtonReplace.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonReplace.Image = Properties.Resources.ToolReplace;
            toolStripButtonReplace.ImageTransparentColor = Color.Magenta;
            toolStripButtonReplace.Name = "toolStripButtonReplace";
            toolStripButtonReplace.Size = new Size(23, 22);
            toolStripButtonReplace.Text = "Replace";
            toolStripButtonReplace.Click += toolStripButtonReplace_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 25);
            // 
            // toolStripButtonUndo
            // 
            toolStripButtonUndo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonUndo.Image = Properties.Resources.ToolUndo;
            toolStripButtonUndo.ImageTransparentColor = Color.Magenta;
            toolStripButtonUndo.Name = "toolStripButtonUndo";
            toolStripButtonUndo.Size = new Size(23, 22);
            toolStripButtonUndo.Text = "Undo";
            toolStripButtonUndo.Click += toolStripButtonUndo_Click;
            // 
            // toolStripButtonRedo
            // 
            toolStripButtonRedo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonRedo.Image = Properties.Resources.ToolRedo;
            toolStripButtonRedo.ImageTransparentColor = Color.Magenta;
            toolStripButtonRedo.Name = "toolStripButtonRedo";
            toolStripButtonRedo.Size = new Size(23, 22);
            toolStripButtonRedo.Text = "Redo";
            toolStripButtonRedo.Click += toolStripButtonRedo_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 25);
            // 
            // toolStripButtonCut
            // 
            toolStripButtonCut.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonCut.Image = Properties.Resources.ToolCut;
            toolStripButtonCut.ImageTransparentColor = Color.Magenta;
            toolStripButtonCut.Name = "toolStripButtonCut";
            toolStripButtonCut.Size = new Size(23, 22);
            toolStripButtonCut.Text = "Cut";
            toolStripButtonCut.Click += toolStripButtonCut_Click;
            // 
            // toolStripButtonCopy
            // 
            toolStripButtonCopy.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonCopy.Image = Properties.Resources.ToolCopy;
            toolStripButtonCopy.ImageTransparentColor = Color.Magenta;
            toolStripButtonCopy.Name = "toolStripButtonCopy";
            toolStripButtonCopy.Size = new Size(23, 22);
            toolStripButtonCopy.Text = "Copy";
            toolStripButtonCopy.Click += toolStripButtonCopy_Click;
            // 
            // toolStripButtonPaste
            // 
            toolStripButtonPaste.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonPaste.Image = Properties.Resources.ToolPaste;
            toolStripButtonPaste.ImageTransparentColor = Color.Magenta;
            toolStripButtonPaste.Name = "toolStripButtonPaste";
            toolStripButtonPaste.Size = new Size(23, 22);
            toolStripButtonPaste.Text = "Paste";
            toolStripButtonPaste.Click += toolStripButtonPaste_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(6, 25);
            // 
            // toolStripButtonDecreaseIndent
            // 
            toolStripButtonDecreaseIndent.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonDecreaseIndent.Image = Properties.Resources.ToolDecreaseIndent;
            toolStripButtonDecreaseIndent.ImageTransparentColor = Color.Magenta;
            toolStripButtonDecreaseIndent.Name = "toolStripButtonDecreaseIndent";
            toolStripButtonDecreaseIndent.Size = new Size(23, 22);
            toolStripButtonDecreaseIndent.Text = "Decrease Indent";
            toolStripButtonDecreaseIndent.Click += toolStripButtonDecreaseIndent_Click;
            // 
            // toolStripButtonIncreaseIndent
            // 
            toolStripButtonIncreaseIndent.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonIncreaseIndent.Image = Properties.Resources.ToolIncreaseIndent;
            toolStripButtonIncreaseIndent.ImageTransparentColor = Color.Magenta;
            toolStripButtonIncreaseIndent.Name = "toolStripButtonIncreaseIndent";
            toolStripButtonIncreaseIndent.Size = new Size(23, 22);
            toolStripButtonIncreaseIndent.Text = "Increase Indent";
            toolStripButtonIncreaseIndent.Click += toolStripButtonIncreaseIndent_Click;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(6, 25);
            // 
            // toolStripButtonProject
            // 
            toolStripButtonProject.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonProject.Image = Properties.Resources.ToolProjectPanel;
            toolStripButtonProject.ImageTransparentColor = Color.Magenta;
            toolStripButtonProject.Name = "toolStripButtonProject";
            toolStripButtonProject.Size = new Size(23, 22);
            toolStripButtonProject.Text = "Toggle Project";
            toolStripButtonProject.Click += toolStripButtonProject_Click;
            // 
            // toolStripButtonOutput
            // 
            toolStripButtonOutput.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonOutput.Image = Properties.Resources.ToolOutputPanel;
            toolStripButtonOutput.ImageTransparentColor = Color.Magenta;
            toolStripButtonOutput.Name = "toolStripButtonOutput";
            toolStripButtonOutput.Size = new Size(23, 22);
            toolStripButtonOutput.Text = "Toggle Output";
            toolStripButtonOutput.Click += toolStripButtonOutput_Click;
            // 
            // toolStripButtonMacros
            // 
            toolStripButtonMacros.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonMacros.Image = Properties.Resources.ToolToolsPanel;
            toolStripButtonMacros.ImageTransparentColor = Color.Magenta;
            toolStripButtonMacros.Name = "toolStripButtonMacros";
            toolStripButtonMacros.Size = new Size(23, 22);
            toolStripButtonMacros.Text = "Toggle Macros";
            toolStripButtonMacros.Visible = false;
            toolStripButtonMacros.Click += toolStripButtonMacros_Click;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new Size(6, 25);
            // 
            // toolStripButtonSnippets
            // 
            toolStripButtonSnippets.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonSnippets.Image = Properties.Resources.ToolSnippets;
            toolStripButtonSnippets.ImageTransparentColor = Color.Magenta;
            toolStripButtonSnippets.Name = "toolStripButtonSnippets";
            toolStripButtonSnippets.Size = new Size(23, 22);
            toolStripButtonSnippets.Text = "Snippets";
            toolStripButtonSnippets.Visible = false;
            toolStripButtonSnippets.Click += toolStripButtonSnippets_Click;
            // 
            // menuStripMain
            // 
            menuStripMain.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, documentToolStripMenuItem, helpToolStripMenuItem });
            menuStripMain.Location = new Point(0, 0);
            menuStripMain.Name = "menuStripMain";
            menuStripMain.Size = new Size(1184, 24);
            menuStripMain.TabIndex = 2;
            menuStripMain.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { connectToolStripMenuItem, closeProjectToolStripMenuItem, toolStripMenuItem2, openToolStripMenuItem, saveToolStripMenuItem1, saveAsToolStripMenuItem, saveAllToolStripMenuItem, toolStripMenuItem1, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // connectToolStripMenuItem
            // 
            connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            connectToolStripMenuItem.Size = new Size(133, 22);
            connectToolStripMenuItem.Text = "Connect";
            connectToolStripMenuItem.Click += connectToolStripMenuItem_Click;
            // 
            // closeProjectToolStripMenuItem
            // 
            closeProjectToolStripMenuItem.Name = "closeProjectToolStripMenuItem";
            closeProjectToolStripMenuItem.Size = new Size(133, 22);
            closeProjectToolStripMenuItem.Text = "Disconnect";
            closeProjectToolStripMenuItem.Click += disconnectToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(130, 6);
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(133, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem1
            // 
            saveToolStripMenuItem1.Name = "saveToolStripMenuItem1";
            saveToolStripMenuItem1.Size = new Size(133, 22);
            saveToolStripMenuItem1.Text = "Save";
            saveToolStripMenuItem1.Click += saveToolStripMenuItem1_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(133, 22);
            saveAsToolStripMenuItem.Text = "Save As";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // saveAllToolStripMenuItem
            // 
            saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            saveAllToolStripMenuItem.Size = new Size(133, 22);
            saveAllToolStripMenuItem.Text = "Save All";
            saveAllToolStripMenuItem.Click += saveAllToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(130, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(133, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // documentToolStripMenuItem
            // 
            documentToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { saveToolStripMenuItem, closeToolStripMenuItem, closeAllToolStripMenuItem });
            documentToolStripMenuItem.Name = "documentToolStripMenuItem";
            documentToolStripMenuItem.Size = new Size(75, 20);
            documentToolStripMenuItem.Text = "Document";
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(120, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(120, 22);
            closeToolStripMenuItem.Text = "Close";
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click;
            // 
            // closeAllToolStripMenuItem
            // 
            closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
            closeAllToolStripMenuItem.Size = new Size(120, 22);
            closeAllToolStripMenuItem.Text = "Close All";
            closeAllToolStripMenuItem.Click += closeAllToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(107, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // FormStudio
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 761);
            Controls.Add(splitContainerObjectExplorer);
            Controls.Add(toolStripButtons);
            Controls.Add(menuStripMain);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStripMain;
            Name = "FormStudio";
            Text = "Katzebase";
            Load += FormStudio_Load;
            ResizeEnd += FormStudio_ResizeEnd;
            DragDrop += FormStudio_DragDrop;
            DragEnter += FormStudio_DragEnter;
            splitContainerObjectExplorer.Panel1.ResumeLayout(false);
            splitContainerObjectExplorer.Panel2.ResumeLayout(false);
            splitContainerObjectExplorer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerObjectExplorer).EndInit();
            splitContainerObjectExplorer.ResumeLayout(false);
            splitContainerMacros.Panel1.ResumeLayout(false);
            splitContainerMacros.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMacros).EndInit();
            splitContainerMacros.ResumeLayout(false);
            statusStripDocument.ResumeLayout(false);
            statusStripDocument.PerformLayout();
            toolStripButtons.ResumeLayout(false);
            toolStripButtons.PerformLayout();
            menuStripMain.ResumeLayout(false);
            menuStripMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SplitContainer splitContainerObjectExplorer;
        private TabControl tabControlBody;
        private ToolStrip toolStripButtons;
        private TreeView treeViewProject;
        private ToolStripButton toolStripButtonSave;
        private ToolStripButton toolStripButtonSaveAll;
        private ToolStripButton toolStripButtonCloseCurrentTab;
        private MenuStrip menuStripMain;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem closeProjectToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem saveAllToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem documentToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripMenuItem closeAllToolStripMenuItem;
        private ToolStripButton toolStripButtonFind;
        private ToolStripButton toolStripButtonReplace;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton toolStripButtonRedo;
        private ToolStripButton toolStripButtonUndo;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton toolStripButtonCut;
        private ToolStripButton toolStripButtonCopy;
        private ToolStripButton toolStripButtonPaste;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripButton toolStripButtonIncreaseIndent;
        private ToolStripButton toolStripButtonDecreaseIndent;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripButton toolStripButtonMacros;
        private ToolStripButton toolStripButtonSnippets;
        private SplitContainer splitContainerMacros;
        private TreeView treeViewMacros;
        private ToolStripButton toolStripButtonNewFile;
        private ToolStripButton toolStripButtonProject;
        private ToolStripButton toolStripButtonOutput;
        private ToolStripSeparator toolStripSeparator7;
        private ToolStripButton toolStripButtonExecuteScript;
        private ToolStripButton toolStripButtonExplainPlan;
        private ToolStripMenuItem connectToolStripMenuItem;
        private StatusStrip statusStripDocument;
        private ToolStripStatusLabel toolStripStatusLabelServerName;
        private ToolStripStatusLabel toolStripStatusLabelProcessId;
        private ToolStripButton toolStripButtonOpen;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem1;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripButton toolStripButtonStop;
    }
}