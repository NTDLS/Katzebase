namespace Katzebase.UI
{
    partial class FormWelcome
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormWelcome));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.listBoxRecent = new System.Windows.Forms.ListBox();
            this.radioButtonOpenRecent = new System.Windows.Forms.RadioButton();
            this.radioButtonOpenExisting = new System.Windows.Forms.RadioButton();
            this.radioButtonCreateNew = new System.Windows.Forms.RadioButton();
            this.radButtonOk = new System.Windows.Forms.Button();
            this.radButtonCancel = new System.Windows.Forms.Button();
            this.checkBoxDontShowAgain = new System.Windows.Forms.CheckBox();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.listBoxRecent);
            this.groupBox2.Controls.Add(this.radioButtonOpenRecent);
            this.groupBox2.Controls.Add(this.radioButtonOpenExisting);
            this.groupBox2.Controls.Add(this.radioButtonCreateNew);
            this.groupBox2.Location = new System.Drawing.Point(14, 14);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox2.Size = new System.Drawing.Size(388, 351);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Create New or Open Existing";
            // 
            // listBoxRecent
            // 
            this.listBoxRecent.ItemHeight = 15;
            this.listBoxRecent.Location = new System.Drawing.Point(7, 105);
            this.listBoxRecent.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.listBoxRecent.Name = "listBoxRecent";
            this.listBoxRecent.Size = new System.Drawing.Size(374, 229);
            this.listBoxRecent.TabIndex = 4;
            // 
            // radioButtonOpenRecent
            // 
            this.radioButtonOpenRecent.Location = new System.Drawing.Point(7, 77);
            this.radioButtonOpenRecent.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonOpenRecent.Name = "radioButtonOpenRecent";
            this.radioButtonOpenRecent.Size = new System.Drawing.Size(156, 21);
            this.radioButtonOpenRecent.TabIndex = 2;
            this.radioButtonOpenRecent.Text = "Open a recent project?";
            this.radioButtonOpenRecent.CheckedChanged += new System.EventHandler(this.radioButtonOpenRecent_CheckedChanged);
            // 
            // radioButtonOpenExisting
            // 
            this.radioButtonOpenExisting.Location = new System.Drawing.Point(7, 50);
            this.radioButtonOpenExisting.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonOpenExisting.Name = "radioButtonOpenExisting";
            this.radioButtonOpenExisting.Size = new System.Drawing.Size(173, 21);
            this.radioButtonOpenExisting.TabIndex = 1;
            this.radioButtonOpenExisting.Text = "Open an existing project?";
            // 
            // radioButtonCreateNew
            // 
            this.radioButtonCreateNew.Checked = true;
            this.radioButtonCreateNew.Location = new System.Drawing.Point(7, 22);
            this.radioButtonCreateNew.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonCreateNew.Name = "radioButtonCreateNew";
            this.radioButtonCreateNew.Size = new System.Drawing.Size(149, 21);
            this.radioButtonCreateNew.TabIndex = 0;
            this.radioButtonCreateNew.TabStop = true;
            this.radioButtonCreateNew.Text = "Create a new project?";
            // 
            // radButtonOk
            // 
            this.radButtonOk.Location = new System.Drawing.Point(206, 372);
            this.radButtonOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radButtonOk.Name = "radButtonOk";
            this.radButtonOk.Size = new System.Drawing.Size(94, 28);
            this.radButtonOk.TabIndex = 4;
            this.radButtonOk.Text = "Ok";
            this.radButtonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // radButtonCancel
            // 
            this.radButtonCancel.Location = new System.Drawing.Point(308, 372);
            this.radButtonCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radButtonCancel.Name = "radButtonCancel";
            this.radButtonCancel.Size = new System.Drawing.Size(94, 28);
            this.radButtonCancel.TabIndex = 5;
            this.radButtonCancel.Text = "Cancel";
            this.radButtonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // checkBoxDontShowAgain
            // 
            this.checkBoxDontShowAgain.Location = new System.Drawing.Point(14, 372);
            this.checkBoxDontShowAgain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxDontShowAgain.Name = "checkBoxDontShowAgain";
            this.checkBoxDontShowAgain.Size = new System.Drawing.Size(155, 21);
            this.checkBoxDontShowAgain.TabIndex = 6;
            this.checkBoxDontShowAgain.Text = "Don\'t show this again?";
            // 
            // FormWelcome
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(421, 410);
            this.Controls.Add(this.checkBoxDontShowAgain);
            this.Controls.Add(this.radButtonOk);
            this.Controls.Add(this.radButtonCancel);
            this.Controls.Add(this.groupBox2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormWelcome";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Welcome";
            this.Load += new System.EventHandler(this.FormWelcome_Load);
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox2;
        private RadioButton radioButtonOpenRecent;
        private RadioButton radioButtonOpenExisting;
        private RadioButton radioButtonCreateNew;
        private Button radButtonOk;
        private Button radButtonCancel;
        private CheckBox checkBoxDontShowAgain;
        private ListBox listBoxRecent;
    }
}