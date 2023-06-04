namespace Katzebase.UI
{
    partial class FormConnect
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConnect));
            groupBox2 = new GroupBox();
            radButtonConnect = new Button();
            radButtonCancel = new Button();
            SuspendLayout();
            // 
            // groupBox2
            // 
            groupBox2.Location = new Point(14, 14);
            groupBox2.Margin = new Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 3, 4, 3);
            groupBox2.Size = new Size(399, 149);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Connect to Katzebase instance";
            // 
            // radButtonConnect
            // 
            radButtonConnect.Location = new Point(217, 169);
            radButtonConnect.Margin = new Padding(4, 3, 4, 3);
            radButtonConnect.Name = "radButtonConnect";
            radButtonConnect.Size = new Size(94, 28);
            radButtonConnect.TabIndex = 4;
            radButtonConnect.Text = "Connect";
            radButtonConnect.Click += buttonOk_Click;
            // 
            // radButtonCancel
            // 
            radButtonCancel.Location = new Point(319, 169);
            radButtonCancel.Margin = new Padding(4, 3, 4, 3);
            radButtonCancel.Name = "radButtonCancel";
            radButtonCancel.Size = new Size(94, 28);
            radButtonCancel.TabIndex = 5;
            radButtonCancel.Text = "Cancel";
            radButtonCancel.Click += buttonCancel_Click;
            // 
            // FormConnect
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(424, 208);
            Controls.Add(radButtonConnect);
            Controls.Add(radButtonCancel);
            Controls.Add(groupBox2);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormConnect";
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Connect";
            ResumeLayout(false);
        }

        #endregion
        private GroupBox groupBox2;
        private Button radButtonConnect;
        private Button radButtonCancel;
    }
}