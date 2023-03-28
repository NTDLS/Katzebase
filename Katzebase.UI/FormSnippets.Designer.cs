namespace Katzebase.UI
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
            this.splitContainerBody = new System.Windows.Forms.SplitContainer();
            this.treeViewSnippets = new System.Windows.Forms.TreeView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBody)).BeginInit();
            this.splitContainerBody.Panel1.SuspendLayout();
            this.splitContainerBody.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerBody
            // 
            this.splitContainerBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerBody.Location = new System.Drawing.Point(0, 0);
            this.splitContainerBody.Name = "splitContainerBody";
            // 
            // splitContainerBody.Panel1
            // 
            this.splitContainerBody.Panel1.Controls.Add(this.treeViewSnippets);
            this.splitContainerBody.Size = new System.Drawing.Size(800, 450);
            this.splitContainerBody.SplitterDistance = 266;
            this.splitContainerBody.TabIndex = 0;
            // 
            // treeViewSnippets
            // 
            this.treeViewSnippets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewSnippets.Location = new System.Drawing.Point(0, 0);
            this.treeViewSnippets.Name = "treeViewSnippets";
            this.treeViewSnippets.Size = new System.Drawing.Size(266, 450);
            this.treeViewSnippets.TabIndex = 0;
            // 
            // FormSnippets
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainerBody);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormSnippets";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Snippets";
            this.Load += new System.EventHandler(this.FormSnippets_Load);
            this.splitContainerBody.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBody)).EndInit();
            this.splitContainerBody.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private SplitContainer splitContainerBody;
        private TreeView treeViewSnippets;
    }
}