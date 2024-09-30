namespace NTDLS.Katzebase.Management
{
    partial class FormFindText
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormFindText));
            buttonFindNext = new Button();
            buttonFind = new Button();
            textBoxFindText = new TextBox();
            label1 = new Label();
            buttonClose = new Button();
            SuspendLayout();
            // 
            // buttonFindNext
            // 
            buttonFindNext.Location = new Point(315, 56);
            buttonFindNext.Name = "buttonFindNext";
            buttonFindNext.Size = new Size(75, 23);
            buttonFindNext.TabIndex = 1;
            buttonFindNext.Text = "Find Next";
            buttonFindNext.UseVisualStyleBackColor = true;
            buttonFindNext.Click += ButtonFindNext_Click;
            // 
            // buttonFind
            // 
            buttonFind.Location = new Point(234, 56);
            buttonFind.Name = "buttonFind";
            buttonFind.Size = new Size(75, 23);
            buttonFind.TabIndex = 2;
            buttonFind.Text = "Find";
            buttonFind.UseVisualStyleBackColor = true;
            buttonFind.Click += ButtonFind_Click;
            // 
            // textBoxFindText
            // 
            textBoxFindText.Location = new Point(12, 27);
            textBoxFindText.Name = "textBoxFindText";
            textBoxFindText.Size = new Size(474, 23);
            textBoxFindText.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 9);
            label1.Name = "label1";
            label1.Size = new Size(54, 15);
            label1.TabIndex = 3;
            label1.Text = "Find Text";
            // 
            // buttonClose
            // 
            buttonClose.Location = new Point(411, 56);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(75, 23);
            buttonClose.TabIndex = 3;
            buttonClose.Text = "Close";
            buttonClose.UseVisualStyleBackColor = true;
            buttonClose.Click += ButtonClose_Click;
            // 
            // FormFindText
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(498, 98);
            Controls.Add(buttonClose);
            Controls.Add(label1);
            Controls.Add(textBoxFindText);
            Controls.Add(buttonFind);
            Controls.Add(buttonFindNext);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "FormFindText";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Find";
            Load += FormFind_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonFindNext;
        private Button buttonFind;
        private TextBox textBoxFindText;
        private Label label1;
        private Button buttonClose;
    }
}