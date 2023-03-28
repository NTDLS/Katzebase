namespace Katzebase.UI
{
    partial class FormAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAbout));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.cmdOk = new System.Windows.Forms.Button();
            this.listViewVersions = new System.Windows.Forms.ListView();
            this.columnHeaderApplication = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderVersion = new System.Windows.Forms.ColumnHeader();
            this.linkWebsite = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(14, 14);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(153, 150);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // cmdOk
            // 
            this.cmdOk.Location = new System.Drawing.Point(419, 171);
            this.cmdOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmdOk.Name = "cmdOk";
            this.cmdOk.Size = new System.Drawing.Size(88, 27);
            this.cmdOk.TabIndex = 7;
            this.cmdOk.Text = "Ok";
            this.cmdOk.UseVisualStyleBackColor = true;
            // 
            // listViewVersions
            // 
            this.listViewVersions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderApplication,
            this.columnHeaderVersion});
            this.listViewVersions.FullRowSelect = true;
            this.listViewVersions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listViewVersions.Location = new System.Drawing.Point(174, 14);
            this.listViewVersions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.listViewVersions.MultiSelect = false;
            this.listViewVersions.Name = "listViewVersions";
            this.listViewVersions.Size = new System.Drawing.Size(332, 149);
            this.listViewVersions.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listViewVersions.TabIndex = 8;
            this.listViewVersions.UseCompatibleStateImageBehavior = false;
            this.listViewVersions.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderApplication
            // 
            this.columnHeaderApplication.Text = "Application";
            this.columnHeaderApplication.Width = 100;
            // 
            // columnHeaderVersion
            // 
            this.columnHeaderVersion.Text = "Version";
            this.columnHeaderVersion.Width = 75;
            // 
            // linkWebsite
            // 
            this.linkWebsite.AutoSize = true;
            this.linkWebsite.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.linkWebsite.Location = new System.Drawing.Point(170, 171);
            this.linkWebsite.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkWebsite.Name = "linkWebsite";
            this.linkWebsite.Size = new System.Drawing.Size(164, 18);
            this.linkWebsite.TabIndex = 9;
            this.linkWebsite.TabStop = true;
            this.linkWebsite.Text = "www.NetworkDLS.com";
            this.linkWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkWebsite_LinkClicked);
            // 
            // FormAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(517, 210);
            this.Controls.Add(this.linkWebsite);
            this.Controls.Add(this.listViewVersions);
            this.Controls.Add(this.cmdOk);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormAbout";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Katzebase.UI : About";
            this.Load += new System.EventHandler(this.FormAbout_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button cmdOk;
        private System.Windows.Forms.ListView listViewVersions;
        private System.Windows.Forms.ColumnHeader columnHeaderApplication;
        private System.Windows.Forms.ColumnHeader columnHeaderVersion;
        private System.Windows.Forms.LinkLabel linkWebsite;
    }
}