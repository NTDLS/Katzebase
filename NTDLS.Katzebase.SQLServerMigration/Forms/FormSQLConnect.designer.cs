namespace NTDLS.Katzebase.SQLServerMigration
{
    partial class FormSQLConnect
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSQLConnect));
            cmdOk = new Button();
            cmdCancel = new Button();
            checkBoxIntegratedSecurity = new CheckBox();
            label1 = new Label();
            label2 = new Label();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            label3 = new Label();
            groupBox1 = new GroupBox();
            textBoxServer = new TextBox();
            checkBoxSSLConnection = new CheckBox();
            label4 = new Label();
            comboBoxDatabaseName = new ComboBox();
            GroupBoxLine = new GroupBox();
            pictureBox1 = new PictureBox();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // cmdOk
            // 
            cmdOk.Location = new Point(150, 332);
            cmdOk.Margin = new Padding(4, 3, 4, 3);
            cmdOk.Name = "cmdOk";
            cmdOk.Size = new Size(88, 27);
            cmdOk.TabIndex = 1;
            cmdOk.Text = "Ok";
            cmdOk.UseVisualStyleBackColor = true;
            cmdOk.Click += cmdOk_Click;
            // 
            // cmdCancel
            // 
            cmdCancel.Location = new Point(245, 332);
            cmdCancel.Margin = new Padding(4, 3, 4, 3);
            cmdCancel.Name = "cmdCancel";
            cmdCancel.Size = new Size(88, 27);
            cmdCancel.TabIndex = 2;
            cmdCancel.Text = "Cancel";
            cmdCancel.UseVisualStyleBackColor = true;
            cmdCancel.Click += cmdCancel_Click;
            // 
            // checkBoxIntegratedSecurity
            // 
            checkBoxIntegratedSecurity.Checked = true;
            checkBoxIntegratedSecurity.CheckState = CheckState.Checked;
            checkBoxIntegratedSecurity.Location = new Point(10, 177);
            checkBoxIntegratedSecurity.Margin = new Padding(4, 3, 4, 3);
            checkBoxIntegratedSecurity.Name = "checkBoxIntegratedSecurity";
            checkBoxIntegratedSecurity.Size = new Size(166, 21);
            checkBoxIntegratedSecurity.TabIndex = 6;
            checkBoxIntegratedSecurity.Text = "Use Integrated Security?";
            checkBoxIntegratedSecurity.CheckedChanged += cbIntegratedSecurity_CheckedChanged;
            // 
            // label1
            // 
            label1.Location = new Point(7, 82);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(69, 21);
            label1.TabIndex = 2;
            label1.Text = "Username:";
            // 
            // label2
            // 
            label2.Location = new Point(7, 127);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(62, 21);
            label2.TabIndex = 4;
            label2.Text = "Password";
            // 
            // txtUsername
            // 
            txtUsername.Enabled = false;
            txtUsername.Location = new Point(10, 102);
            txtUsername.Margin = new Padding(4, 3, 4, 3);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(223, 23);
            txtUsername.TabIndex = 3;
            // 
            // txtPassword
            // 
            txtPassword.Enabled = false;
            txtPassword.Location = new Point(10, 147);
            txtPassword.Margin = new Padding(4, 3, 4, 3);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(223, 23);
            txtPassword.TabIndex = 5;
            // 
            // label3
            // 
            label3.Location = new Point(7, 25);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(85, 21);
            label3.TabIndex = 0;
            label3.Text = "Server Name:";
            // 
            // groupBox1
            // 
            groupBox1.AccessibleRole = AccessibleRole.Grouping;
            groupBox1.Controls.Add(textBoxServer);
            groupBox1.Controls.Add(checkBoxSSLConnection);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(comboBoxDatabaseName);
            groupBox1.Controls.Add(GroupBoxLine);
            groupBox1.Controls.Add(txtUsername);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(checkBoxIntegratedSecurity);
            groupBox1.Controls.Add(txtPassword);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(label2);
            groupBox1.Location = new Point(82, 15);
            groupBox1.Margin = new Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 3, 4, 3);
            groupBox1.Size = new Size(251, 306);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "SQL Server Connectivity Attributes";
            // 
            // textBoxServer
            // 
            textBoxServer.Location = new Point(10, 47);
            textBoxServer.Name = "textBoxServer";
            textBoxServer.Size = new Size(224, 23);
            textBoxServer.TabIndex = 4;
            textBoxServer.Text = "127.0.0.1";
            // 
            // checkBoxSSLConnection
            // 
            checkBoxSSLConnection.Location = new Point(10, 203);
            checkBoxSSLConnection.Margin = new Padding(4, 3, 4, 3);
            checkBoxSSLConnection.Name = "checkBoxSSLConnection";
            checkBoxSSLConnection.Size = new Size(178, 21);
            checkBoxSSLConnection.TabIndex = 10;
            checkBoxSSLConnection.Text = "Encrypt Connection (SSL) ?";
            // 
            // label4
            // 
            label4.Location = new Point(7, 250);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(103, 21);
            label4.TabIndex = 8;
            label4.Text = "Database Name:";
            // 
            // comboBoxDatabaseName
            // 
            comboBoxDatabaseName.Location = new Point(10, 271);
            comboBoxDatabaseName.Margin = new Padding(4, 3, 4, 3);
            comboBoxDatabaseName.Name = "comboBoxDatabaseName";
            comboBoxDatabaseName.Size = new Size(223, 23);
            comboBoxDatabaseName.TabIndex = 9;
            // 
            // GroupBoxLine
            // 
            GroupBoxLine.AccessibleRole = AccessibleRole.Grouping;
            GroupBoxLine.Location = new Point(10, 233);
            GroupBoxLine.Margin = new Padding(4, 3, 4, 3);
            GroupBoxLine.Name = "GroupBoxLine";
            GroupBoxLine.Padding = new Padding(4, 3, 4, 3);
            GroupBoxLine.Size = new Size(224, 2);
            GroupBoxLine.TabIndex = 7;
            GroupBoxLine.TabStop = false;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.Login;
            pictureBox1.Location = new Point(14, 42);
            pictureBox1.Margin = new Padding(4, 3, 4, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(61, 65);
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // FormSQLConnect
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(365, 369);
            Controls.Add(pictureBox1);
            Controls.Add(groupBox1);
            Controls.Add(cmdCancel);
            Controls.Add(cmdOk);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormSQLConnect";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Connect to SQL Server";
            Load += SQLConnectForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button cmdOk;
        private Button cmdCancel;
        private CheckBox checkBoxIntegratedSecurity;
        private Label label1;
        private Label label2;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Label label3;
        private GroupBox groupBox1;
        private PictureBox pictureBox1;
        private Label label4;
        private ComboBox comboBoxDatabaseName;
        private GroupBox GroupBoxLine;
        private CheckBox checkBoxSSLConnection;
        private TextBox textBoxServer;
    }
}