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
            listViewSQLServer = new ListView();
            columnHeaderSchema = new ColumnHeader();
            columnHeaderTable = new ColumnHeader();
            columnHeaderStatus = new ColumnHeader();
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
            menuStripBody.SuspendLayout();
            SuspendLayout();
            // 
            // listViewSQLServer
            // 
            listViewSQLServer.CheckBoxes = true;
            listViewSQLServer.Columns.AddRange(new ColumnHeader[] { columnHeaderSchema, columnHeaderTable, columnHeaderStatus });
            listViewSQLServer.FullRowSelect = true;
            listViewSQLServer.GridLines = true;
            listViewSQLServer.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listViewSQLServer.Location = new Point(12, 156);
            listViewSQLServer.Name = "listViewSQLServer";
            listViewSQLServer.Size = new Size(758, 329);
            listViewSQLServer.TabIndex = 2;
            listViewSQLServer.UseCompatibleStateImageBehavior = false;
            listViewSQLServer.View = View.Details;
            // 
            // columnHeaderSchema
            // 
            columnHeaderSchema.Text = "Schema";
            columnHeaderSchema.Width = 200;
            // 
            // columnHeaderTable
            // 
            columnHeaderTable.Text = "Table";
            columnHeaderTable.Width = 400;
            // 
            // columnHeaderStatus
            // 
            columnHeaderStatus.Text = "Status";
            columnHeaderStatus.Width = 120;
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
            textBoxServerHost.Text = "http://localhost:6858/";
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
            buttonImport.Location = new Point(680, 53);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(90, 90);
            buttonImport.TabIndex = 3;
            buttonImport.Text = "Start Import";
            buttonImport.UseVisualStyleBackColor = false;
            buttonImport.Click += buttonImport_Click;
            // 
            // menuStripBody
            // 
            menuStripBody.Items.AddRange(new ToolStripItem[] { connectionToolStripMenuItem, helpToolStripMenuItem });
            menuStripBody.Location = new Point(0, 0);
            menuStripBody.Name = "menuStripBody";
            menuStripBody.Size = new Size(782, 24);
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
            changeConnectionToolStripMenuItem.Click += changeConnectionToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
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
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
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
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(782, 498);
            Controls.Add(labelBoxServerPort);
            Controls.Add(textBoxServerPort);
            Controls.Add(buttonImport);
            Controls.Add(label3);
            Controls.Add(textBoxServerSchema);
            Controls.Add(label2);
            Controls.Add(textBoxServerHost);
            Controls.Add(label1);
            Controls.Add(listViewSQLServer);
            Controls.Add(menuStripBody);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStripBody;
            MaximizeBox = false;
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Katzebase MSSQL Migration";
            Resize += FormMain_Resize;
            menuStripBody.ResumeLayout(false);
            menuStripBody.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listViewSQLServer;
        private ColumnHeader columnHeaderSchema;
        private ColumnHeader columnHeaderTable;
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
        private ColumnHeader columnHeaderStatus;
        private TextBox textBoxServerPort;
        private Label labelBoxServerPort;
    }
}