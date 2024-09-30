namespace NTDLS.Katzebase.Management
{
    partial class FormSnippets
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSnippets));
            splitContainerBody = new SplitContainer();
            treeViewSnippets = new TreeView();
            ((System.ComponentModel.ISupportInitialize)splitContainerBody).BeginInit();
            splitContainerBody.Panel1.SuspendLayout();
            splitContainerBody.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerBody
            // 
            splitContainerBody.Dock = DockStyle.Fill;
            splitContainerBody.Location = new Point(0, 0);
            splitContainerBody.Name = "splitContainerBody";
            // 
            // splitContainerBody.Panel1
            // 
            splitContainerBody.Panel1.Controls.Add(treeViewSnippets);
            splitContainerBody.Size = new Size(800, 450);
            splitContainerBody.SplitterDistance = 266;
            splitContainerBody.TabIndex = 0;
            // 
            // treeViewSnippets
            // 
            treeViewSnippets.Dock = DockStyle.Fill;
            treeViewSnippets.Location = new Point(0, 0);
            treeViewSnippets.Name = "treeViewSnippets";
            treeViewSnippets.Size = new Size(266, 450);
            treeViewSnippets.TabIndex = 0;
            // 
            // FormSnippets
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(splitContainerBody);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormSnippets";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Snippets";
            Load += FormSnippets_Load;
            splitContainerBody.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerBody).EndInit();
            splitContainerBody.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainerBody;
        private TreeView treeViewSnippets;
    }
}