namespace NTDLS.Katzebase.ObjectViewer
{
    partial class FormMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            textBoxFile = new TextBox();
            labelFile = new Label();
            labelType = new Label();
            textBoxObject = new TextBox();
            buttonBrowse = new Button();
            textBoxType = new TextBox();
            SuspendLayout();
            // 
            // textBoxFile
            // 
            textBoxFile.Location = new Point(49, 18);
            textBoxFile.Name = "textBoxFile";
            textBoxFile.Size = new Size(391, 23);
            textBoxFile.TabIndex = 0;
            // 
            // labelFile
            // 
            labelFile.AutoSize = true;
            labelFile.Location = new Point(12, 21);
            labelFile.Name = "labelFile";
            labelFile.Size = new Size(25, 15);
            labelFile.TabIndex = 1;
            labelFile.Text = "File";
            // 
            // labelType
            // 
            labelType.AutoSize = true;
            labelType.Location = new Point(12, 58);
            labelType.Name = "labelType";
            labelType.Size = new Size(31, 15);
            labelType.TabIndex = 2;
            labelType.Text = "Type";
            // 
            // textBoxObject
            // 
            textBoxObject.Location = new Point(49, 98);
            textBoxObject.Multiline = true;
            textBoxObject.Name = "textBoxObject";
            textBoxObject.ScrollBars = ScrollBars.Both;
            textBoxObject.Size = new Size(720, 369);
            textBoxObject.TabIndex = 4;
            textBoxObject.WordWrap = false;
            // 
            // buttonBrowse
            // 
            buttonBrowse.Location = new Point(446, 17);
            buttonBrowse.Name = "buttonBrowse";
            buttonBrowse.Size = new Size(75, 23);
            buttonBrowse.TabIndex = 5;
            buttonBrowse.Text = "Browse";
            buttonBrowse.UseVisualStyleBackColor = true;
            buttonBrowse.Click += ButtonBrowse_Click;
            // 
            // textBoxType
            // 
            textBoxType.Location = new Point(49, 55);
            textBoxType.Name = "textBoxType";
            textBoxType.ReadOnly = true;
            textBoxType.Size = new Size(391, 23);
            textBoxType.TabIndex = 6;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(781, 479);
            Controls.Add(textBoxType);
            Controls.Add(buttonBrowse);
            Controls.Add(textBoxObject);
            Controls.Add(labelType);
            Controls.Add(labelFile);
            Controls.Add(textBoxFile);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormMain";
            SizeGripStyle = SizeGripStyle.Show;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Katzebase Object Viewer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxFile;
        private Label labelFile;
        private Label labelType;
        private TextBox textBoxObject;
        private Button buttonBrowse;
        private TextBox textBoxType;
    }
}
