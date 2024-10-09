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
            buttonFind_FindNext = new Button();
            textBoxFindText = new TextBox();
            labelFindText = new Label();
            buttonFind_Close = new Button();
            tabControlBody = new TabControl();
            tabPageFind = new TabPage();
            tabPageReplace = new TabPage();
            buttonReplace_Close = new Button();
            buttonReplace_FindNext = new Button();
            buttonReplace_ReplaceAll = new Button();
            buttonReplace_Replace = new Button();
            labelFindReplaceWithText = new Label();
            textBoxFindReplaceText = new TextBox();
            textBoxFindReplaceWithText = new TextBox();
            labelFindReplaceText = new Label();
            tabControlBody.SuspendLayout();
            tabPageFind.SuspendLayout();
            tabPageReplace.SuspendLayout();
            SuspendLayout();
            // 
            // buttonFind_FindNext
            // 
            buttonFind_FindNext.Location = new Point(450, 29);
            buttonFind_FindNext.Name = "buttonFind_FindNext";
            buttonFind_FindNext.Size = new Size(75, 23);
            buttonFind_FindNext.TabIndex = 1;
            buttonFind_FindNext.Text = "Find Next";
            buttonFind_FindNext.UseVisualStyleBackColor = true;
            buttonFind_FindNext.Click += ButtonFindNext_Click;
            // 
            // textBoxFindText
            // 
            textBoxFindText.Location = new Point(96, 30);
            textBoxFindText.Name = "textBoxFindText";
            textBoxFindText.Size = new Size(348, 23);
            textBoxFindText.TabIndex = 0;
            // 
            // labelFindText
            // 
            labelFindText.AutoSize = true;
            labelFindText.Location = new Point(36, 33);
            labelFindText.Name = "labelFindText";
            labelFindText.Size = new Size(54, 15);
            labelFindText.TabIndex = 3;
            labelFindText.Text = "Find Text";
            // 
            // buttonFind_Close
            // 
            buttonFind_Close.Location = new Point(450, 143);
            buttonFind_Close.Name = "buttonFind_Close";
            buttonFind_Close.Size = new Size(75, 23);
            buttonFind_Close.TabIndex = 2;
            buttonFind_Close.Text = "Close";
            buttonFind_Close.UseVisualStyleBackColor = true;
            buttonFind_Close.Click += ButtonClose_Click;
            // 
            // tabControlBody
            // 
            tabControlBody.Controls.Add(tabPageFind);
            tabControlBody.Controls.Add(tabPageReplace);
            tabControlBody.Location = new Point(5, 5);
            tabControlBody.Name = "tabControlBody";
            tabControlBody.SelectedIndex = 0;
            tabControlBody.Size = new Size(550, 204);
            tabControlBody.TabIndex = 4;
            // 
            // tabPageFind
            // 
            tabPageFind.Controls.Add(textBoxFindText);
            tabPageFind.Controls.Add(buttonFind_Close);
            tabPageFind.Controls.Add(labelFindText);
            tabPageFind.Controls.Add(buttonFind_FindNext);
            tabPageFind.Location = new Point(4, 24);
            tabPageFind.Name = "tabPageFind";
            tabPageFind.Padding = new Padding(3);
            tabPageFind.Size = new Size(542, 176);
            tabPageFind.TabIndex = 0;
            tabPageFind.Text = "Find";
            tabPageFind.UseVisualStyleBackColor = true;
            // 
            // tabPageReplace
            // 
            tabPageReplace.Controls.Add(buttonReplace_Close);
            tabPageReplace.Controls.Add(buttonReplace_FindNext);
            tabPageReplace.Controls.Add(buttonReplace_ReplaceAll);
            tabPageReplace.Controls.Add(buttonReplace_Replace);
            tabPageReplace.Controls.Add(labelFindReplaceWithText);
            tabPageReplace.Controls.Add(textBoxFindReplaceText);
            tabPageReplace.Controls.Add(textBoxFindReplaceWithText);
            tabPageReplace.Controls.Add(labelFindReplaceText);
            tabPageReplace.Location = new Point(4, 24);
            tabPageReplace.Name = "tabPageReplace";
            tabPageReplace.Padding = new Padding(3);
            tabPageReplace.Size = new Size(542, 176);
            tabPageReplace.TabIndex = 1;
            tabPageReplace.Text = "Replace";
            tabPageReplace.UseVisualStyleBackColor = true;
            // 
            // buttonReplace_Close
            // 
            buttonReplace_Close.Location = new Point(450, 143);
            buttonReplace_Close.Name = "buttonReplace_Close";
            buttonReplace_Close.Size = new Size(75, 23);
            buttonReplace_Close.TabIndex = 5;
            buttonReplace_Close.Text = "Close";
            buttonReplace_Close.UseVisualStyleBackColor = true;
            buttonReplace_Close.Click += buttonReplace_Close_Click;
            // 
            // buttonReplace_FindNext
            // 
            buttonReplace_FindNext.Location = new Point(450, 29);
            buttonReplace_FindNext.Name = "buttonReplace_FindNext";
            buttonReplace_FindNext.Size = new Size(75, 23);
            buttonReplace_FindNext.TabIndex = 2;
            buttonReplace_FindNext.Text = "Find Next";
            buttonReplace_FindNext.UseVisualStyleBackColor = true;
            buttonReplace_FindNext.Click += buttonReplace_FindNext_Click;
            // 
            // buttonReplace_ReplaceAll
            // 
            buttonReplace_ReplaceAll.Location = new Point(450, 87);
            buttonReplace_ReplaceAll.Name = "buttonReplace_ReplaceAll";
            buttonReplace_ReplaceAll.Size = new Size(75, 23);
            buttonReplace_ReplaceAll.TabIndex = 4;
            buttonReplace_ReplaceAll.Text = "Replace All";
            buttonReplace_ReplaceAll.UseVisualStyleBackColor = true;
            buttonReplace_ReplaceAll.Click += buttonReplace_ReplaceAll_Click;
            // 
            // buttonReplace_Replace
            // 
            buttonReplace_Replace.Location = new Point(450, 58);
            buttonReplace_Replace.Name = "buttonReplace_Replace";
            buttonReplace_Replace.Size = new Size(75, 23);
            buttonReplace_Replace.TabIndex = 3;
            buttonReplace_Replace.Text = "Replace";
            buttonReplace_Replace.UseVisualStyleBackColor = true;
            buttonReplace_Replace.Click += buttonReplace_Replace_Click;
            // 
            // labelFindReplaceWithText
            // 
            labelFindReplaceWithText.AutoSize = true;
            labelFindReplaceWithText.Location = new Point(14, 62);
            labelFindReplaceWithText.Name = "labelFindReplaceWithText";
            labelFindReplaceWithText.Size = new Size(76, 15);
            labelFindReplaceWithText.TabIndex = 11;
            labelFindReplaceWithText.Text = "Replace With";
            // 
            // textBoxFindReplaceText
            // 
            textBoxFindReplaceText.Location = new Point(96, 30);
            textBoxFindReplaceText.Name = "textBoxFindReplaceText";
            textBoxFindReplaceText.Size = new Size(348, 23);
            textBoxFindReplaceText.TabIndex = 0;
            // 
            // textBoxFindReplaceWithText
            // 
            textBoxFindReplaceWithText.Location = new Point(96, 59);
            textBoxFindReplaceWithText.Name = "textBoxFindReplaceWithText";
            textBoxFindReplaceWithText.Size = new Size(348, 23);
            textBoxFindReplaceWithText.TabIndex = 1;
            // 
            // labelFindReplaceText
            // 
            labelFindReplaceText.AutoSize = true;
            labelFindReplaceText.Location = new Point(36, 33);
            labelFindReplaceText.Name = "labelFindReplaceText";
            labelFindReplaceText.Size = new Size(54, 15);
            labelFindReplaceText.TabIndex = 10;
            labelFindReplaceText.Text = "Find Text";
            // 
            // FormFindText
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(558, 212);
            Controls.Add(tabControlBody);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "FormFindText";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Find";
            Load += FormFind_Load;
            tabControlBody.ResumeLayout(false);
            tabPageFind.ResumeLayout(false);
            tabPageFind.PerformLayout();
            tabPageReplace.ResumeLayout(false);
            tabPageReplace.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button buttonFind_FindNext;
        private TextBox textBoxFindText;
        private Label labelFindText;
        private Button buttonFind_Close;
        private TabControl tabControlBody;
        private TabPage tabPageFind;
        private TabPage tabPageReplace;
        private Button buttonReplace_ReplaceAll;
        private Button buttonReplace_Replace;
        private Label labelFindReplaceWithText;
        private TextBox textBoxFindReplaceWithText;
        private Label labelFindReplaceText;
        private TextBox textBoxFindReplaceText;
        private Button buttonReplace_FindNext;
        private Button buttonReplace_Close;
    }
}