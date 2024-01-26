using NTDLS.Katzebase.SQLServerMigration.Controls;

namespace NTDLS.Katzebase.SQLServerMigration
{
    partial class FormProgress
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormProgress));
            buttonCancel = new Button();
            pbProgress = new ProgressBar();
            lblHeader = new Label();
            lblBody = new Label();
            SuspendLayout();
            // 
            // buttonCancel
            // 
            buttonCancel.Enabled = false;
            buttonCancel.Location = new Point(286, 120);
            buttonCancel.Margin = new Padding(4, 3, 4, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(88, 27);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += ButtonCancel_Click;
            // 
            // pbProgress
            // 
            pbProgress.Location = new Point(59, 87);
            pbProgress.Margin = new Padding(4, 3, 4, 3);
            pbProgress.Name = "pbProgress";
            pbProgress.Size = new Size(314, 27);
            pbProgress.Style = ProgressBarStyle.Marquee;
            pbProgress.TabIndex = 2;
            // 
            // lblHeader
            // 
            lblHeader.AutoEllipsis = true;
            lblHeader.Location = new Point(56, 14);
            lblHeader.Margin = new Padding(4, 0, 4, 0);
            lblHeader.Name = "lblHeader";
            lblHeader.Size = new Size(317, 38);
            lblHeader.TabIndex = 3;
            lblHeader.Text = "Please wait...";
            // 
            // lblBody
            // 
            lblBody.AutoEllipsis = true;
            lblBody.Location = new Point(56, 52);
            lblBody.Margin = new Padding(4, 0, 4, 0);
            lblBody.Name = "lblBody";
            lblBody.Size = new Size(317, 31);
            lblBody.TabIndex = 4;
            lblBody.Text = "Please wait...";
            // 
            // FormProgress
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(387, 160);
            ControlBox = false;
            Controls.Add(lblBody);
            Controls.Add(lblHeader);
            Controls.Add(pbProgress);
            Controls.Add(buttonCancel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormProgress";
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Please wait...";
            Shown += FormProgress_Shown;
            ResumeLayout(false);
        }

        #endregion

        private Button buttonCancel;
        private ProgressBar pbProgress;
        private Label lblHeader;
        private Label lblBody;
        private SpinningProgress spinningProgress1;
    }
}