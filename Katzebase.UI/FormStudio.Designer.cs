namespace Katzebase.UI
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
            this.splitContainerProject = new System.Windows.Forms.SplitContainer();
            this.treeViewProject = new System.Windows.Forms.TreeView();
            this.splitContainerMacros = new System.Windows.Forms.SplitContainer();
            this.tabControlBody = new System.Windows.Forms.TabControl();
            this.treeViewMacros = new System.Windows.Forms.TreeView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonNewProject = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSaveAll = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonCloseCurrentTab = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonPreview = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExecuteScript = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExecuteProject = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonFind = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonReplace = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonRedo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonUndo = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonCut = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonPaste = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonDecreaseIndent = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonIncreaseIndent = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonProject = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonOutput = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonMacros = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonSnippets = new System.Windows.Forms.ToolStripButton();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openExistingProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.saveAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.documentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainerOutput = new System.Windows.Forms.SplitContainer();
            this.tabControlOutput = new System.Windows.Forms.TabControl();
            this.tabPagePreview = new System.Windows.Forms.TabPage();
            this.tabPageOutput = new System.Windows.Forms.TabPage();
            this.richTextBoxOutput = new System.Windows.Forms.RichTextBox();
            this.tabPageResults = new System.Windows.Forms.TabPage();
            this.dataGridViewResults = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerProject)).BeginInit();
            this.splitContainerProject.Panel1.SuspendLayout();
            this.splitContainerProject.Panel2.SuspendLayout();
            this.splitContainerProject.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMacros)).BeginInit();
            this.splitContainerMacros.Panel1.SuspendLayout();
            this.splitContainerMacros.Panel2.SuspendLayout();
            this.splitContainerMacros.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerOutput)).BeginInit();
            this.splitContainerOutput.Panel1.SuspendLayout();
            this.splitContainerOutput.Panel2.SuspendLayout();
            this.splitContainerOutput.SuspendLayout();
            this.tabControlOutput.SuspendLayout();
            this.tabPageOutput.SuspendLayout();
            this.tabPageResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainerProject
            // 
            this.splitContainerProject.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerProject.Location = new System.Drawing.Point(54, 30);
            this.splitContainerProject.Name = "splitContainerProject";
            // 
            // splitContainerProject.Panel1
            // 
            this.splitContainerProject.Panel1.Controls.Add(this.treeViewProject);
            // 
            // splitContainerProject.Panel2
            // 
            this.splitContainerProject.Panel2.Controls.Add(this.splitContainerMacros);
            this.splitContainerProject.Size = new System.Drawing.Size(921, 349);
            this.splitContainerProject.SplitterDistance = 320;
            this.splitContainerProject.TabIndex = 0;
            // 
            // treeViewProject
            // 
            this.treeViewProject.Location = new System.Drawing.Point(14, 35);
            this.treeViewProject.Name = "treeViewProject";
            this.treeViewProject.Size = new System.Drawing.Size(256, 292);
            this.treeViewProject.TabIndex = 0;
            // 
            // splitContainerMacros
            // 
            this.splitContainerMacros.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerMacros.Location = new System.Drawing.Point(44, 35);
            this.splitContainerMacros.Name = "splitContainerMacros";
            // 
            // splitContainerMacros.Panel1
            // 
            this.splitContainerMacros.Panel1.Controls.Add(this.tabControlBody);
            // 
            // splitContainerMacros.Panel2
            // 
            this.splitContainerMacros.Panel2.Controls.Add(this.treeViewMacros);
            this.splitContainerMacros.Size = new System.Drawing.Size(529, 236);
            this.splitContainerMacros.SplitterDistance = 261;
            this.splitContainerMacros.TabIndex = 1;
            // 
            // tabControlBody
            // 
            this.tabControlBody.Location = new System.Drawing.Point(24, 22);
            this.tabControlBody.Name = "tabControlBody";
            this.tabControlBody.SelectedIndex = 0;
            this.tabControlBody.Size = new System.Drawing.Size(216, 189);
            this.tabControlBody.TabIndex = 0;
            // 
            // treeViewMacros
            // 
            this.treeViewMacros.Location = new System.Drawing.Point(13, 22);
            this.treeViewMacros.Name = "treeViewMacros";
            this.treeViewMacros.Size = new System.Drawing.Size(168, 189);
            this.treeViewMacros.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonNewProject,
            this.toolStripButtonSave,
            this.toolStripButtonSaveAll,
            this.toolStripButtonCloseCurrentTab,
            this.toolStripSeparator1,
            this.toolStripButtonPreview,
            this.toolStripButtonExecuteScript,
            this.toolStripButtonExecuteProject,
            this.toolStripSeparator2,
            this.toolStripButtonFind,
            this.toolStripButtonReplace,
            this.toolStripSeparator3,
            this.toolStripButtonRedo,
            this.toolStripButtonUndo,
            this.toolStripSeparator4,
            this.toolStripButtonCut,
            this.toolStripButtonCopy,
            this.toolStripButtonPaste,
            this.toolStripSeparator5,
            this.toolStripButtonDecreaseIndent,
            this.toolStripButtonIncreaseIndent,
            this.toolStripSeparator6,
            this.toolStripButtonProject,
            this.toolStripButtonOutput,
            this.toolStripButtonMacros,
            this.toolStripSeparator7,
            this.toolStripButtonSnippets});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1032, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonNewProject
            // 
            this.toolStripButtonNewProject.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNewProject.Image = global::Katzebase.UI.Properties.Resources.ToolNewProject;
            this.toolStripButtonNewProject.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNewProject.Name = "toolStripButtonNewProject";
            this.toolStripButtonNewProject.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonNewProject.Text = "New Project";
            this.toolStripButtonNewProject.Click += new System.EventHandler(this.toolStripButtonNewProject_Click);
            // 
            // toolStripButtonSave
            // 
            this.toolStripButtonSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSave.Image = global::Katzebase.UI.Properties.Resources.ToolSave;
            this.toolStripButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSave.Name = "toolStripButtonSave";
            this.toolStripButtonSave.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSave.Text = "Save";
            this.toolStripButtonSave.Click += new System.EventHandler(this.toolStripButtonSave_Click);
            // 
            // toolStripButtonSaveAll
            // 
            this.toolStripButtonSaveAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSaveAll.Image = global::Katzebase.UI.Properties.Resources.ToolSaveAll;
            this.toolStripButtonSaveAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSaveAll.Name = "toolStripButtonSaveAll";
            this.toolStripButtonSaveAll.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSaveAll.Text = "Save All";
            this.toolStripButtonSaveAll.Click += new System.EventHandler(this.toolStripButtonSaveAll_Click);
            // 
            // toolStripButtonCloseCurrentTab
            // 
            this.toolStripButtonCloseCurrentTab.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCloseCurrentTab.Image = global::Katzebase.UI.Properties.Resources.ToolCloseFile;
            this.toolStripButtonCloseCurrentTab.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCloseCurrentTab.Name = "toolStripButtonCloseCurrentTab";
            this.toolStripButtonCloseCurrentTab.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonCloseCurrentTab.Text = "Close Current Tab";
            this.toolStripButtonCloseCurrentTab.Click += new System.EventHandler(this.toolStripButtonCloseCurrentTab_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonPreview
            // 
            this.toolStripButtonPreview.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonPreview.Image = global::Katzebase.UI.Properties.Resources.GetSQL;
            this.toolStripButtonPreview.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonPreview.Name = "toolStripButtonPreview";
            this.toolStripButtonPreview.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonPreview.Text = "Preview";
            // 
            // toolStripButtonExecuteScript
            // 
            this.toolStripButtonExecuteScript.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonExecuteScript.Image = global::Katzebase.UI.Properties.Resources.ToolRunOne;
            this.toolStripButtonExecuteScript.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExecuteScript.Name = "toolStripButtonExecuteScript";
            this.toolStripButtonExecuteScript.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonExecuteScript.Text = "Execute Script";
            this.toolStripButtonExecuteScript.Click += new System.EventHandler(this.toolStripButtonExecuteScript_Click);
            // 
            // toolStripButtonExecuteProject
            // 
            this.toolStripButtonExecuteProject.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonExecuteProject.Image = global::Katzebase.UI.Properties.Resources.ToolRun;
            this.toolStripButtonExecuteProject.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExecuteProject.Name = "toolStripButtonExecuteProject";
            this.toolStripButtonExecuteProject.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonExecuteProject.Text = "Execute Project";
            this.toolStripButtonExecuteProject.Click += new System.EventHandler(this.toolStripButtonExecuteProject_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonFind
            // 
            this.toolStripButtonFind.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonFind.Image = global::Katzebase.UI.Properties.Resources.ToolFind;
            this.toolStripButtonFind.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonFind.Name = "toolStripButtonFind";
            this.toolStripButtonFind.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonFind.Text = "Find";
            this.toolStripButtonFind.Click += new System.EventHandler(this.toolStripButtonFind_Click);
            // 
            // toolStripButtonReplace
            // 
            this.toolStripButtonReplace.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonReplace.Image = global::Katzebase.UI.Properties.Resources.ToolReplace;
            this.toolStripButtonReplace.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonReplace.Name = "toolStripButtonReplace";
            this.toolStripButtonReplace.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonReplace.Text = "Replace";
            this.toolStripButtonReplace.Click += new System.EventHandler(this.toolStripButtonReplace_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonRedo
            // 
            this.toolStripButtonRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRedo.Image = global::Katzebase.UI.Properties.Resources.ToolRedo;
            this.toolStripButtonRedo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRedo.Name = "toolStripButtonRedo";
            this.toolStripButtonRedo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRedo.Text = "Redo";
            this.toolStripButtonRedo.Click += new System.EventHandler(this.toolStripButtonRedo_Click);
            // 
            // toolStripButtonUndo
            // 
            this.toolStripButtonUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonUndo.Image = global::Katzebase.UI.Properties.Resources.ToolUndo;
            this.toolStripButtonUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonUndo.Name = "toolStripButtonUndo";
            this.toolStripButtonUndo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonUndo.Text = "Undo";
            this.toolStripButtonUndo.Click += new System.EventHandler(this.toolStripButtonUndo_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonCut
            // 
            this.toolStripButtonCut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCut.Image = global::Katzebase.UI.Properties.Resources.ToolCut;
            this.toolStripButtonCut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCut.Name = "toolStripButtonCut";
            this.toolStripButtonCut.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonCut.Text = "Cut";
            this.toolStripButtonCut.Click += new System.EventHandler(this.toolStripButtonCut_Click);
            // 
            // toolStripButtonCopy
            // 
            this.toolStripButtonCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCopy.Image = global::Katzebase.UI.Properties.Resources.ToolCopy;
            this.toolStripButtonCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCopy.Name = "toolStripButtonCopy";
            this.toolStripButtonCopy.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonCopy.Text = "Copy";
            this.toolStripButtonCopy.Click += new System.EventHandler(this.toolStripButtonCopy_Click);
            // 
            // toolStripButtonPaste
            // 
            this.toolStripButtonPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonPaste.Image = global::Katzebase.UI.Properties.Resources.ToolPaste;
            this.toolStripButtonPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonPaste.Name = "toolStripButtonPaste";
            this.toolStripButtonPaste.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonPaste.Text = "Paste";
            this.toolStripButtonPaste.Click += new System.EventHandler(this.toolStripButtonPaste_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonDecreaseIndent
            // 
            this.toolStripButtonDecreaseIndent.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDecreaseIndent.Image = global::Katzebase.UI.Properties.Resources.ToolDecreaseIndent;
            this.toolStripButtonDecreaseIndent.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDecreaseIndent.Name = "toolStripButtonDecreaseIndent";
            this.toolStripButtonDecreaseIndent.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonDecreaseIndent.Text = "Decrease Indent";
            this.toolStripButtonDecreaseIndent.Click += new System.EventHandler(this.toolStripButtonDecreaseIndent_Click);
            // 
            // toolStripButtonIncreaseIndent
            // 
            this.toolStripButtonIncreaseIndent.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonIncreaseIndent.Image = global::Katzebase.UI.Properties.Resources.ToolIncreaseIndent;
            this.toolStripButtonIncreaseIndent.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonIncreaseIndent.Name = "toolStripButtonIncreaseIndent";
            this.toolStripButtonIncreaseIndent.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonIncreaseIndent.Text = "Increase Indent";
            this.toolStripButtonIncreaseIndent.Click += new System.EventHandler(this.toolStripButtonIncreaseIndent_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonProject
            // 
            this.toolStripButtonProject.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonProject.Image = global::Katzebase.UI.Properties.Resources.ToolProjectPanel;
            this.toolStripButtonProject.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonProject.Name = "toolStripButtonProject";
            this.toolStripButtonProject.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonProject.Text = "Toggle Project";
            this.toolStripButtonProject.Click += new System.EventHandler(this.toolStripButtonProject_Click);
            // 
            // toolStripButtonOutput
            // 
            this.toolStripButtonOutput.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonOutput.Image = global::Katzebase.UI.Properties.Resources.ToolOutputPanel;
            this.toolStripButtonOutput.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOutput.Name = "toolStripButtonOutput";
            this.toolStripButtonOutput.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonOutput.Text = "Toggle Output";
            this.toolStripButtonOutput.Click += new System.EventHandler(this.toolStripButtonOutput_Click);
            // 
            // toolStripButtonMacros
            // 
            this.toolStripButtonMacros.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMacros.Image = global::Katzebase.UI.Properties.Resources.ToolToolsPanel;
            this.toolStripButtonMacros.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonMacros.Name = "toolStripButtonMacros";
            this.toolStripButtonMacros.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonMacros.Text = "Toggle Macros";
            this.toolStripButtonMacros.Click += new System.EventHandler(this.toolStripButtonMacros_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonSnippets
            // 
            this.toolStripButtonSnippets.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSnippets.Image = global::Katzebase.UI.Properties.Resources.ToolExamples;
            this.toolStripButtonSnippets.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSnippets.Name = "toolStripButtonSnippets";
            this.toolStripButtonSnippets.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSnippets.Text = "Snippets";
            this.toolStripButtonSnippets.Click += new System.EventHandler(this.toolStripButtonSnippets_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.documentToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1032, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newProjectToolStripMenuItem,
            this.openExistingProjectToolStripMenuItem,
            this.closeProjectToolStripMenuItem,
            this.toolStripMenuItem2,
            this.saveAllToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newProjectToolStripMenuItem
            // 
            this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.newProjectToolStripMenuItem.Text = "New Project";
            this.newProjectToolStripMenuItem.Click += new System.EventHandler(this.newProjectToolStripMenuItem_Click);
            // 
            // openExistingProjectToolStripMenuItem
            // 
            this.openExistingProjectToolStripMenuItem.Name = "openExistingProjectToolStripMenuItem";
            this.openExistingProjectToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.openExistingProjectToolStripMenuItem.Text = "Open Existing Project";
            this.openExistingProjectToolStripMenuItem.Click += new System.EventHandler(this.openExistingProjectToolStripMenuItem_Click);
            // 
            // closeProjectToolStripMenuItem
            // 
            this.closeProjectToolStripMenuItem.Name = "closeProjectToolStripMenuItem";
            this.closeProjectToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.closeProjectToolStripMenuItem.Text = "Close Project";
            this.closeProjectToolStripMenuItem.Click += new System.EventHandler(this.closeProjectToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(184, 6);
            // 
            // saveAllToolStripMenuItem
            // 
            this.saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            this.saveAllToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.saveAllToolStripMenuItem.Text = "Save All";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(184, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // documentToolStripMenuItem
            // 
            this.documentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.closeAllToolStripMenuItem});
            this.documentToolStripMenuItem.Name = "documentToolStripMenuItem";
            this.documentToolStripMenuItem.Size = new System.Drawing.Size(75, 20);
            this.documentToolStripMenuItem.Text = "Document";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // closeAllToolStripMenuItem
            // 
            this.closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
            this.closeAllToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
            this.closeAllToolStripMenuItem.Text = "Close All";
            this.closeAllToolStripMenuItem.Click += new System.EventHandler(this.closeAllToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // splitContainerOutput
            // 
            this.splitContainerOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerOutput.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerOutput.Location = new System.Drawing.Point(0, 49);
            this.splitContainerOutput.Name = "splitContainerOutput";
            this.splitContainerOutput.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerOutput.Panel1
            // 
            this.splitContainerOutput.Panel1.Controls.Add(this.splitContainerProject);
            // 
            // splitContainerOutput.Panel2
            // 
            this.splitContainerOutput.Panel2.Controls.Add(this.tabControlOutput);
            this.splitContainerOutput.Size = new System.Drawing.Size(1032, 597);
            this.splitContainerOutput.SplitterDistance = 403;
            this.splitContainerOutput.TabIndex = 3;
            // 
            // tabControlOutput
            // 
            this.tabControlOutput.Controls.Add(this.tabPagePreview);
            this.tabControlOutput.Controls.Add(this.tabPageOutput);
            this.tabControlOutput.Controls.Add(this.tabPageResults);
            this.tabControlOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlOutput.Location = new System.Drawing.Point(0, 0);
            this.tabControlOutput.Name = "tabControlOutput";
            this.tabControlOutput.SelectedIndex = 0;
            this.tabControlOutput.Size = new System.Drawing.Size(1032, 190);
            this.tabControlOutput.TabIndex = 0;
            // 
            // tabPagePreview
            // 
            this.tabPagePreview.Location = new System.Drawing.Point(4, 24);
            this.tabPagePreview.Name = "tabPagePreview";
            this.tabPagePreview.Size = new System.Drawing.Size(1024, 162);
            this.tabPagePreview.TabIndex = 0;
            this.tabPagePreview.Text = "Preview";
            this.tabPagePreview.UseVisualStyleBackColor = true;
            // 
            // tabPageOutput
            // 
            this.tabPageOutput.Controls.Add(this.richTextBoxOutput);
            this.tabPageOutput.Location = new System.Drawing.Point(4, 24);
            this.tabPageOutput.Name = "tabPageOutput";
            this.tabPageOutput.Size = new System.Drawing.Size(1024, 162);
            this.tabPageOutput.TabIndex = 1;
            this.tabPageOutput.Text = "Output";
            this.tabPageOutput.UseVisualStyleBackColor = true;
            // 
            // richTextBoxOutput
            // 
            this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxOutput.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxOutput.Name = "richTextBoxOutput";
            this.richTextBoxOutput.Size = new System.Drawing.Size(1024, 162);
            this.richTextBoxOutput.TabIndex = 0;
            this.richTextBoxOutput.Text = "";
            // 
            // tabPageResults
            // 
            this.tabPageResults.Controls.Add(this.dataGridViewResults);
            this.tabPageResults.Location = new System.Drawing.Point(4, 24);
            this.tabPageResults.Name = "tabPageResults";
            this.tabPageResults.Size = new System.Drawing.Size(1024, 162);
            this.tabPageResults.TabIndex = 2;
            this.tabPageResults.Text = "Results";
            this.tabPageResults.UseVisualStyleBackColor = true;
            // 
            // dataGridViewResults
            // 
            this.dataGridViewResults.AllowUserToAddRows = false;
            this.dataGridViewResults.AllowUserToDeleteRows = false;
            this.dataGridViewResults.AllowUserToOrderColumns = true;
            this.dataGridViewResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewResults.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewResults.Name = "dataGridViewResults";
            this.dataGridViewResults.ReadOnly = true;
            this.dataGridViewResults.RowTemplate.Height = 25;
            this.dataGridViewResults.Size = new System.Drawing.Size(1024, 162);
            this.dataGridViewResults.TabIndex = 1;
            // 
            // FormStudio
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1032, 646);
            this.Controls.Add(this.splitContainerOutput);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormStudio";
            this.Text = "Katzebase";
            this.Load += new System.EventHandler(this.FormStudio_Load);
            this.splitContainerProject.Panel1.ResumeLayout(false);
            this.splitContainerProject.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerProject)).EndInit();
            this.splitContainerProject.ResumeLayout(false);
            this.splitContainerMacros.Panel1.ResumeLayout(false);
            this.splitContainerMacros.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMacros)).EndInit();
            this.splitContainerMacros.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainerOutput.Panel1.ResumeLayout(false);
            this.splitContainerOutput.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerOutput)).EndInit();
            this.splitContainerOutput.ResumeLayout(false);
            this.tabControlOutput.ResumeLayout(false);
            this.tabPageOutput.ResumeLayout(false);
            this.tabPageResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SplitContainer splitContainerProject;
        private TabControl tabControlBody;
        private ToolStrip toolStrip1;
        private TreeView treeViewProject;
        private ToolStripButton toolStripButtonSave;
        private ToolStripButton toolStripButtonSaveAll;
        private ToolStripButton toolStripButtonCloseCurrentTab;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newProjectToolStripMenuItem;
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
        private SplitContainer splitContainerOutput;
        private TabControl tabControlOutput;
        private ToolStripButton toolStripButtonExecuteProject;
        private ToolStripMenuItem closeAllToolStripMenuItem;
        private ToolStripMenuItem openExistingProjectToolStripMenuItem;
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
        private ToolStripButton toolStripButtonNewProject;
        private ToolStripButton toolStripButtonPreview;
        private ToolStripButton toolStripButtonProject;
        private ToolStripButton toolStripButtonOutput;
        private ToolStripSeparator toolStripSeparator7;
        private TabPage tabPagePreview;
        private TabPage tabPageOutput;
        private RichTextBox richTextBoxOutput;
        private ToolStripButton toolStripButtonExecuteScript;
        private TabPage tabPageResults;
        private DataGridView dataGridViewResults;
    }
}