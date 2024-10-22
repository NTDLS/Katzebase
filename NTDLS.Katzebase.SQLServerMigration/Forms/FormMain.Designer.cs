namespace NTDLS.Katzebase.SQLServerMigration
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
            label1 = new Label();
            textBoxServerHost = new TextBox();
            textBoxServerSchema = new TextBox();
            label2 = new Label();
            label3 = new Label();
            buttonImport = new Button();
            menuStripBody = new MenuStrip();
            connectionToolStripMenuItem = new ToolStripMenuItem();
            changeConnectionToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            textBoxServerPort = new TextBox();
            labelBoxServerPort = new Label();
            labelPassword = new Label();
            textBoxPassword = new TextBox();
            textBoxUsername = new TextBox();
            labelUsername = new Label();
            dataGridViewSqlServer = new DataGridView();
            ColumnImportData = new DataGridViewCheckBoxColumn();
            ColumnImportIndexes = new DataGridViewCheckBoxColumn();
            ColumnSourceTable = new DataGridViewTextBoxColumn();
            ColumnAnalysis = new DataGridViewTextBoxColumn();
            ColumnTargetSchema = new DataGridViewTextBoxColumn();
            ColumnPageSize = new DataGridViewTextBoxColumn();
            ColumnStatus = new DataGridViewTextBoxColumn();
            menuStripBody.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSqlServer).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 35);
            label1.Name = "label1";
            label1.Size = new Size(90, 15);
            label1.TabIndex = 1;
            label1.Text = "Katzebase Host:";
            // 
            // textBoxServerHost
            // 
            textBoxServerHost.Location = new Point(12, 53);
            textBoxServerHost.Name = "textBoxServerHost";
            textBoxServerHost.Size = new Size(196, 23);
            textBoxServerHost.TabIndex = 0;
            textBoxServerHost.Text = "localhost";
            // 
            // textBoxServerSchema
            // 
            textBoxServerSchema.Location = new Point(12, 100);
            textBoxServerSchema.Name = "textBoxServerSchema";
            textBoxServerSchema.Size = new Size(196, 23);
            textBoxServerSchema.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 82);
            label2.Name = "label2";
            label2.Size = new Size(84, 15);
            label2.TabIndex = 3;
            label2.Text = "Target Schema";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 138);
            label3.Name = "label3";
            label3.Size = new Size(151, 15);
            label3.TabIndex = 5;
            label3.Text = "SQL Server Tables to Import";
            // 
            // buttonImport
            // 
            buttonImport.BackColor = Color.FromArgb(192, 255, 192);
            buttonImport.ForeColor = SystemColors.ControlText;
            buttonImport.Location = new Point(727, 53);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(90, 90);
            buttonImport.TabIndex = 3;
            buttonImport.Text = "Start";
            buttonImport.UseVisualStyleBackColor = false;
            buttonImport.Click += buttonImport_Click;
            // 
            // menuStripBody
            // 
            menuStripBody.Items.AddRange(new ToolStripItem[] { connectionToolStripMenuItem, helpToolStripMenuItem });
            menuStripBody.Location = new Point(0, 0);
            menuStripBody.Name = "menuStripBody";
            menuStripBody.Size = new Size(828, 24);
            menuStripBody.TabIndex = 6;
            menuStripBody.Text = "menuStrip1";
            // 
            // connectionToolStripMenuItem
            // 
            connectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { changeConnectionToolStripMenuItem, exitToolStripMenuItem });
            connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
            connectionToolStripMenuItem.Size = new Size(81, 20);
            connectionToolStripMenuItem.Text = "Connection";
            // 
            // changeConnectionToolStripMenuItem
            // 
            changeConnectionToolStripMenuItem.Name = "changeConnectionToolStripMenuItem";
            changeConnectionToolStripMenuItem.Size = new Size(180, 22);
            changeConnectionToolStripMenuItem.Text = "Change Connection";
            changeConnectionToolStripMenuItem.Click += ChangeConnectionToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(107, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += AboutToolStripMenuItem_Click;
            // 
            // textBoxServerPort
            // 
            textBoxServerPort.Location = new Point(214, 53);
            textBoxServerPort.Name = "textBoxServerPort";
            textBoxServerPort.Size = new Size(65, 23);
            textBoxServerPort.TabIndex = 7;
            textBoxServerPort.Text = "6858";
            // 
            // labelBoxServerPort
            // 
            labelBoxServerPort.AutoSize = true;
            labelBoxServerPort.Location = new Point(214, 35);
            labelBoxServerPort.Name = "labelBoxServerPort";
            labelBoxServerPort.Size = new Size(29, 15);
            labelBoxServerPort.TabIndex = 8;
            labelBoxServerPort.Text = "Port";
            // 
            // labelPassword
            // 
            labelPassword.AutoSize = true;
            labelPassword.Location = new Point(285, 82);
            labelPassword.Name = "labelPassword";
            labelPassword.Size = new Size(57, 15);
            labelPassword.TabIndex = 12;
            labelPassword.Text = "Password";
            // 
            // textBoxPassword
            // 
            textBoxPassword.Location = new Point(285, 100);
            textBoxPassword.Name = "textBoxPassword";
            textBoxPassword.PasswordChar = '*';
            textBoxPassword.Size = new Size(196, 23);
            textBoxPassword.TabIndex = 11;
            // 
            // textBoxUsername
            // 
            textBoxUsername.Location = new Point(285, 53);
            textBoxUsername.Name = "textBoxUsername";
            textBoxUsername.Size = new Size(196, 23);
            textBoxUsername.TabIndex = 9;
            textBoxUsername.Text = "admin";
            // 
            // labelUsername
            // 
            labelUsername.AutoSize = true;
            labelUsername.Location = new Point(285, 35);
            labelUsername.Name = "labelUsername";
            labelUsername.Size = new Size(60, 15);
            labelUsername.TabIndex = 10;
            labelUsername.Text = "Username";
            // 
            // dataGridViewSqlServer
            // 
            dataGridViewSqlServer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewSqlServer.Columns.AddRange(new DataGridViewColumn[] { ColumnImportData, ColumnImportIndexes, ColumnSourceTable, ColumnAnalysis, ColumnTargetSchema, ColumnPageSize, ColumnStatus });
            dataGridViewSqlServer.Location = new Point(12, 156);
            dataGridViewSqlServer.Name = "dataGridViewSqlServer";
            dataGridViewSqlServer.Size = new Size(805, 330);
            dataGridViewSqlServer.TabIndex = 13;
            // 
            // ColumnImportData
            // 
            ColumnImportData.HeaderText = "Import Data";
            ColumnImportData.Name = "ColumnImportData";
            ColumnImportData.Width = 50;
            // 
            // ColumnImportIndexes
            // 
            ColumnImportIndexes.HeaderText = "Import Indexes";
            ColumnImportIndexes.Name = "ColumnImportIndexes";
            ColumnImportIndexes.Resizable = DataGridViewTriState.True;
            ColumnImportIndexes.SortMode = DataGridViewColumnSortMode.Automatic;
            // 
            // ColumnSourceTable
            // 
            ColumnSourceTable.HeaderText = "Source Table";
            ColumnSourceTable.Name = "ColumnSourceTable";
            ColumnSourceTable.ReadOnly = true;
            ColumnSourceTable.Width = 175;
            // 
            // ColumnAnalysis
            // 
            ColumnAnalysis.HeaderText = "Analysis";
            ColumnAnalysis.Name = "ColumnAnalysis";
            ColumnAnalysis.ReadOnly = true;
            // 
            // ColumnTargetSchema
            // 
            ColumnTargetSchema.HeaderText = "Target Schema";
            ColumnTargetSchema.Name = "ColumnTargetSchema";
            ColumnTargetSchema.Width = 175;
            // 
            // ColumnPageSize
            // 
            ColumnPageSize.HeaderText = "Page Size";
            ColumnPageSize.Name = "ColumnPageSize";
            // 
            // ColumnStatus
            // 
            ColumnStatus.HeaderText = "Status";
            ColumnStatus.Name = "ColumnStatus";
            ColumnStatus.ReadOnly = true;
            ColumnStatus.Resizable = DataGridViewTriState.True;
            ColumnStatus.SortMode = DataGridViewColumnSortMode.NotSortable;
            ColumnStatus.Width = 250;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(828, 498);
            Controls.Add(dataGridViewSqlServer);
            Controls.Add(labelPassword);
            Controls.Add(textBoxPassword);
            Controls.Add(textBoxUsername);
            Controls.Add(labelUsername);
            Controls.Add(labelBoxServerPort);
            Controls.Add(textBoxServerPort);
            Controls.Add(buttonImport);
            Controls.Add(label3);
            Controls.Add(textBoxServerSchema);
            Controls.Add(label2);
            Controls.Add(textBoxServerHost);
            Controls.Add(label1);
            Controls.Add(menuStripBody);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStripBody;
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Katzebase MSSQL Migration";
            FormClosing += FormMain_FormClosing;
            Load += FormMain_Load;
            Resize += FormMain_Resize;
            menuStripBody.ResumeLayout(false);
            menuStripBody.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSqlServer).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label1;
        private TextBox textBoxServerHost;
        private TextBox textBoxServerSchema;
        private Label label2;
        private Label label3;
        private Button buttonImport;
        private MenuStrip menuStripBody;
        private ToolStripMenuItem connectionToolStripMenuItem;
        private ToolStripMenuItem changeConnectionToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private TextBox textBoxServerPort;
        private Label labelBoxServerPort;
        private Label labelPassword;
        private TextBox textBoxPassword;
        private TextBox textBoxUsername;
        private Label labelUsername;
        private DataGridView dataGridViewSqlServer;
        private DataGridViewCheckBoxColumn ColumnImportData;
        private DataGridViewCheckBoxColumn ColumnImportIndexes;
        private DataGridViewTextBoxColumn ColumnSourceTable;
        private DataGridViewTextBoxColumn ColumnAnalysis;
        private DataGridViewTextBoxColumn ColumnTargetSchema;
        private DataGridViewTextBoxColumn ColumnPageSize;
        private DataGridViewTextBoxColumn ColumnStatus;
    }
}