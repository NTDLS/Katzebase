namespace NTDLS.Katzebase.Management
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
            textBoxPassword = new TextBox();
            textBoxUsername = new TextBox();
            labelPassword = new Label();
            labelUsername = new Label();
            labelServerAddress = new Label();
            labelPort = new Label();
            textBoxPort = new TextBox();
            textBoxServerAddress = new TextBox();
            buttonConnect = new Button();
            buttonCancel = new Button();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBoxPassword);
            groupBox2.Controls.Add(textBoxUsername);
            groupBox2.Controls.Add(labelPassword);
            groupBox2.Controls.Add(labelUsername);
            groupBox2.Controls.Add(labelServerAddress);
            groupBox2.Controls.Add(labelPort);
            groupBox2.Controls.Add(textBoxPort);
            groupBox2.Controls.Add(textBoxServerAddress);
            groupBox2.Location = new Point(14, 14);
            groupBox2.Margin = new Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 3, 4, 3);
            groupBox2.Size = new Size(319, 196);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Connect to Katzebase";
            // 
            // textBoxPassword
            // 
            textBoxPassword.Location = new Point(11, 156);
            textBoxPassword.Name = "textBoxPassword";
            textBoxPassword.PasswordChar = '*';
            textBoxPassword.Size = new Size(290, 23);
            textBoxPassword.TabIndex = 3;
            // 
            // textBoxUsername
            // 
            textBoxUsername.Location = new Point(11, 108);
            textBoxUsername.Name = "textBoxUsername";
            textBoxUsername.Size = new Size(290, 23);
            textBoxUsername.TabIndex = 2;
            // 
            // labelPassword
            // 
            labelPassword.AutoSize = true;
            labelPassword.Location = new Point(11, 138);
            labelPassword.Name = "labelPassword";
            labelPassword.Size = new Size(57, 15);
            labelPassword.TabIndex = 5;
            labelPassword.Text = "Password";
            // 
            // labelUsername
            // 
            labelUsername.AutoSize = true;
            labelUsername.Location = new Point(11, 90);
            labelUsername.Name = "labelUsername";
            labelUsername.Size = new Size(60, 15);
            labelUsername.TabIndex = 4;
            labelUsername.Text = "Username";
            // 
            // labelServerAddress
            // 
            labelServerAddress.AutoSize = true;
            labelServerAddress.Location = new Point(11, 28);
            labelServerAddress.Name = "labelServerAddress";
            labelServerAddress.Size = new Size(84, 15);
            labelServerAddress.TabIndex = 3;
            labelServerAddress.Text = "Server Address";
            // 
            // labelPort
            // 
            labelPort.AutoSize = true;
            labelPort.Location = new Point(245, 28);
            labelPort.Name = "labelPort";
            labelPort.Size = new Size(29, 15);
            labelPort.TabIndex = 2;
            labelPort.Text = "Port";
            // 
            // textBoxPort
            // 
            textBoxPort.Location = new Point(245, 46);
            textBoxPort.Name = "textBoxPort";
            textBoxPort.Size = new Size(56, 23);
            textBoxPort.TabIndex = 1;
            // 
            // textBoxServerAddress
            // 
            textBoxServerAddress.Location = new Point(11, 46);
            textBoxServerAddress.Name = "textBoxServerAddress";
            textBoxServerAddress.Size = new Size(228, 23);
            textBoxServerAddress.TabIndex = 0;
            // 
            // buttonConnect
            // 
            buttonConnect.Location = new Point(137, 216);
            buttonConnect.Margin = new Padding(4, 3, 4, 3);
            buttonConnect.Name = "buttonConnect";
            buttonConnect.Size = new Size(94, 28);
            buttonConnect.TabIndex = 4;
            buttonConnect.Text = "Connect";
            buttonConnect.Click += ButtonOk_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(239, 216);
            buttonCancel.Margin = new Padding(4, 3, 4, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(94, 28);
            buttonCancel.TabIndex = 5;
            buttonCancel.Text = "Cancel";
            buttonCancel.Click += ButtonCancel_Click;
            // 
            // FormConnect
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(344, 259);
            Controls.Add(buttonConnect);
            Controls.Add(buttonCancel);
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
            Load += FormConnect_Load;
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private GroupBox groupBox2;
        private Button buttonConnect;
        private Button buttonCancel;
        private Label labelServerAddress;
        private Label labelPort;
        private TextBox textBoxPort;
        private TextBox textBoxServerAddress;
        private TextBox textBoxPassword;
        private TextBox textBoxUsername;
        private Label labelPassword;
        private Label labelUsername;
    }
}