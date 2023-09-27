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
            listViewSQLServer = new ListView();
            columnHeaderSchema = new ColumnHeader();
            columnHeaderTable = new ColumnHeader();
            label1 = new Label();
            textBoxBKServerAddress = new TextBox();
            textBoxBKServerSchema = new TextBox();
            label2 = new Label();
            label3 = new Label();
            buttonImport = new Button();
            SuspendLayout();
            // 
            // listViewSQLServer
            // 
            listViewSQLServer.CheckBoxes = true;
            listViewSQLServer.Columns.AddRange(new ColumnHeader[] { columnHeaderSchema, columnHeaderTable });
            listViewSQLServer.FullRowSelect = true;
            listViewSQLServer.GridLines = true;
            listViewSQLServer.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listViewSQLServer.Location = new Point(12, 130);
            listViewSQLServer.Name = "listViewSQLServer";
            listViewSQLServer.Size = new Size(681, 329);
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
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(107, 15);
            label1.TabIndex = 1;
            label1.Text = "Katzebase Address:";
            // 
            // textBoxBKServerAddress
            // 
            textBoxBKServerAddress.Location = new Point(12, 27);
            textBoxBKServerAddress.Name = "textBoxBKServerAddress";
            textBoxBKServerAddress.Size = new Size(196, 23);
            textBoxBKServerAddress.TabIndex = 0;
            textBoxBKServerAddress.Text = "http://localhost:6858/";
            // 
            // textBoxBKServerSchema
            // 
            textBoxBKServerSchema.Location = new Point(12, 74);
            textBoxBKServerSchema.Name = "textBoxBKServerSchema";
            textBoxBKServerSchema.Size = new Size(196, 23);
            textBoxBKServerSchema.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 56);
            label2.Name = "label2";
            label2.Size = new Size(84, 15);
            label2.TabIndex = 3;
            label2.Text = "Target Schema";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 112);
            label3.Name = "label3";
            label3.Size = new Size(151, 15);
            label3.TabIndex = 5;
            label3.Text = "SQL Server Tables to Import";
            // 
            // buttonImport
            // 
            buttonImport.BackColor = Color.FromArgb(192, 255, 192);
            buttonImport.ForeColor = SystemColors.ControlText;
            buttonImport.Location = new Point(603, 27);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(90, 90);
            buttonImport.TabIndex = 3;
            buttonImport.Text = "Start Import";
            buttonImport.UseVisualStyleBackColor = false;
            buttonImport.Click += buttonImport_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(705, 473);
            Controls.Add(buttonImport);
            Controls.Add(label3);
            Controls.Add(textBoxBKServerSchema);
            Controls.Add(label2);
            Controls.Add(textBoxBKServerAddress);
            Controls.Add(label1);
            Controls.Add(listViewSQLServer);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "FormMain";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listViewSQLServer;
        private ColumnHeader columnHeaderSchema;
        private ColumnHeader columnHeaderTable;
        private Label label1;
        private TextBox textBoxBKServerAddress;
        private TextBox textBoxBKServerSchema;
        private Label label2;
        private Label label3;
        private Button buttonImport;
    }
}