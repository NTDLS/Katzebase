using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NTDLS.Katzebase.Management
{
    public partial class FormSettings : Form
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

        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(FormSettings));
            labelUIQueryTimeOut = new Label();
            textBoxUIQueryTimeOut = new TextBox();
            textBoxQueryTimeOut = new TextBox();
            labelQueryTimeOut = new Label();
            textBoxMaximumRows = new TextBox();
            labelMaximumRows = new Label();
            buttonSave = new Button();
            buttonCancel = new Button();
            tabControlBody = new TabControl();
            tabPageEditor = new TabPage();
            groupBoxEditorGeneral = new GroupBox();
            checkBoxLineNumbers = new CheckBox();
            checkBoxWordWrap = new CheckBox();
            labelFontAndSize = new Label();
            numericUpDownFontSize = new NumericUpDown();
            panelFontSampleParent = new Panel();
            comboBoxFont = new ComboBox();
            tabPageQuery = new TabPage();
            groupBoxQueryExecution = new GroupBox();
            tabControlBody.SuspendLayout();
            tabPageEditor.SuspendLayout();
            groupBoxEditorGeneral.SuspendLayout();
            ((ISupportInitialize)numericUpDownFontSize).BeginInit();
            tabPageQuery.SuspendLayout();
            groupBoxQueryExecution.SuspendLayout();
            SuspendLayout();
            // 
            // labelUIQueryTimeOut
            // 
            labelUIQueryTimeOut.AutoSize = true;
            labelUIQueryTimeOut.Location = new Point(18, 24);
            labelUIQueryTimeOut.Name = "labelUIQueryTimeOut";
            labelUIQueryTimeOut.Size = new Size(101, 15);
            labelUIQueryTimeOut.TabIndex = 0;
            labelUIQueryTimeOut.Text = "UI query time-out";
            // 
            // textBoxUIQueryTimeOut
            // 
            textBoxUIQueryTimeOut.Location = new Point(125, 21);
            textBoxUIQueryTimeOut.Name = "textBoxUIQueryTimeOut";
            textBoxUIQueryTimeOut.Size = new Size(100, 23);
            textBoxUIQueryTimeOut.TabIndex = 0;
            // 
            // textBoxQueryTimeOut
            // 
            textBoxQueryTimeOut.Location = new Point(125, 50);
            textBoxQueryTimeOut.Name = "textBoxQueryTimeOut";
            textBoxQueryTimeOut.Size = new Size(100, 23);
            textBoxQueryTimeOut.TabIndex = 1;
            // 
            // labelQueryTimeOut
            // 
            labelQueryTimeOut.AutoSize = true;
            labelQueryTimeOut.Location = new Point(6, 53);
            labelQueryTimeOut.Name = "labelQueryTimeOut";
            labelQueryTimeOut.Size = new Size(113, 15);
            labelQueryTimeOut.TabIndex = 2;
            labelQueryTimeOut.Text = "User query time-out";
            // 
            // textBoxMaximumRows
            // 
            textBoxMaximumRows.Location = new Point(125, 79);
            textBoxMaximumRows.Name = "textBoxMaximumRows";
            textBoxMaximumRows.Size = new Size(100, 23);
            textBoxMaximumRows.TabIndex = 2;
            // 
            // labelMaximumRows
            // 
            labelMaximumRows.AutoSize = true;
            labelMaximumRows.Location = new Point(30, 82);
            labelMaximumRows.Name = "labelMaximumRows";
            labelMaximumRows.Size = new Size(90, 15);
            labelMaximumRows.TabIndex = 4;
            labelMaximumRows.Text = "Maximum rows";
            // 
            // buttonSave
            // 
            buttonSave.Location = new Point(551, 305);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(75, 23);
            buttonSave.TabIndex = 3;
            buttonSave.Text = "Save";
            buttonSave.UseVisualStyleBackColor = true;
            buttonSave.Click += ButtonSave_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(632, 305);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.TabIndex = 4;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += ButtonCancel_Click;
            // 
            // tabControlBody
            // 
            tabControlBody.Controls.Add(tabPageEditor);
            tabControlBody.Controls.Add(tabPageQuery);
            tabControlBody.Location = new Point(12, 12);
            tabControlBody.Name = "tabControlBody";
            tabControlBody.SelectedIndex = 0;
            tabControlBody.Size = new Size(699, 287);
            tabControlBody.TabIndex = 5;
            // 
            // tabPageEditor
            // 
            tabPageEditor.Controls.Add(groupBoxEditorGeneral);
            tabPageEditor.Controls.Add(labelFontAndSize);
            tabPageEditor.Controls.Add(numericUpDownFontSize);
            tabPageEditor.Controls.Add(panelFontSampleParent);
            tabPageEditor.Controls.Add(comboBoxFont);
            tabPageEditor.Location = new Point(4, 24);
            tabPageEditor.Name = "tabPageEditor";
            tabPageEditor.Size = new Size(691, 259);
            tabPageEditor.TabIndex = 0;
            tabPageEditor.Text = "Editor";
            tabPageEditor.UseVisualStyleBackColor = true;
            // 
            // groupBoxEditorGeneral
            // 
            groupBoxEditorGeneral.Controls.Add(checkBoxLineNumbers);
            groupBoxEditorGeneral.Controls.Add(checkBoxWordWrap);
            groupBoxEditorGeneral.Location = new Point(8, 8);
            groupBoxEditorGeneral.Name = "groupBoxEditorGeneral";
            groupBoxEditorGeneral.Size = new Size(237, 243);
            groupBoxEditorGeneral.TabIndex = 4;
            groupBoxEditorGeneral.TabStop = false;
            groupBoxEditorGeneral.Text = "General";
            // 
            // checkBoxLineNumbers
            // 
            checkBoxLineNumbers.AutoSize = true;
            checkBoxLineNumbers.Location = new Point(6, 47);
            checkBoxLineNumbers.Name = "checkBoxLineNumbers";
            checkBoxLineNumbers.Size = new Size(105, 19);
            checkBoxLineNumbers.TabIndex = 1;
            checkBoxLineNumbers.Text = "Line Numbers?";
            checkBoxLineNumbers.UseVisualStyleBackColor = true;
            // 
            // checkBoxWordWrap
            // 
            checkBoxWordWrap.AutoSize = true;
            checkBoxWordWrap.Location = new Point(6, 22);
            checkBoxWordWrap.Name = "checkBoxWordWrap";
            checkBoxWordWrap.Size = new Size(89, 19);
            checkBoxWordWrap.TabIndex = 0;
            checkBoxWordWrap.Text = "Word wrap?";
            checkBoxWordWrap.UseVisualStyleBackColor = true;
            // 
            // labelFontAndSize
            // 
            labelFontAndSize.AutoSize = true;
            labelFontAndSize.Location = new Point(251, 8);
            labelFontAndSize.Name = "labelFontAndSize";
            labelFontAndSize.Size = new Size(77, 15);
            labelFontAndSize.TabIndex = 3;
            labelFontAndSize.Text = "Font and Size";
            // 
            // numericUpDownFontSize
            // 
            numericUpDownFontSize.DecimalPlaces = 2;
            numericUpDownFontSize.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
            numericUpDownFontSize.Location = new Point(615, 26);
            numericUpDownFontSize.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            numericUpDownFontSize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDownFontSize.Name = "numericUpDownFontSize";
            numericUpDownFontSize.Size = new Size(62, 23);
            numericUpDownFontSize.TabIndex = 3;
            numericUpDownFontSize.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // panelFontSampleParent
            // 
            panelFontSampleParent.Location = new Point(251, 55);
            panelFontSampleParent.Name = "panelFontSampleParent";
            panelFontSampleParent.Size = new Size(426, 196);
            panelFontSampleParent.TabIndex = 4;
            // 
            // comboBoxFont
            // 
            comboBoxFont.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFont.FormattingEnabled = true;
            comboBoxFont.Location = new Point(251, 26);
            comboBoxFont.Name = "comboBoxFont";
            comboBoxFont.Size = new Size(358, 23);
            comboBoxFont.TabIndex = 2;
            // 
            // tabPageQuery
            // 
            tabPageQuery.Controls.Add(groupBoxQueryExecution);
            tabPageQuery.Location = new Point(4, 24);
            tabPageQuery.Name = "tabPageQuery";
            tabPageQuery.Size = new Size(691, 259);
            tabPageQuery.TabIndex = 1;
            tabPageQuery.Text = "Query";
            tabPageQuery.UseVisualStyleBackColor = true;
            // 
            // groupBoxQueryExecution
            // 
            groupBoxQueryExecution.Controls.Add(textBoxUIQueryTimeOut);
            groupBoxQueryExecution.Controls.Add(labelUIQueryTimeOut);
            groupBoxQueryExecution.Controls.Add(labelMaximumRows);
            groupBoxQueryExecution.Controls.Add(textBoxQueryTimeOut);
            groupBoxQueryExecution.Controls.Add(labelQueryTimeOut);
            groupBoxQueryExecution.Controls.Add(textBoxMaximumRows);
            groupBoxQueryExecution.Location = new Point(8, 8);
            groupBoxQueryExecution.Name = "groupBoxQueryExecution";
            groupBoxQueryExecution.Size = new Size(290, 125);
            groupBoxQueryExecution.TabIndex = 5;
            groupBoxQueryExecution.TabStop = false;
            groupBoxQueryExecution.Text = "Execution";
            // 
            // FormSettings
            // 
            ClientSize = new Size(717, 340);
            Controls.Add(tabControlBody);
            Controls.Add(buttonCancel);
            Controls.Add(buttonSave);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormSettings";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            tabControlBody.ResumeLayout(false);
            tabPageEditor.ResumeLayout(false);
            tabPageEditor.PerformLayout();
            groupBoxEditorGeneral.ResumeLayout(false);
            groupBoxEditorGeneral.PerformLayout();
            ((ISupportInitialize)numericUpDownFontSize).EndInit();
            tabPageQuery.ResumeLayout(false);
            groupBoxQueryExecution.ResumeLayout(false);
            groupBoxQueryExecution.PerformLayout();
            ResumeLayout(false);
        }

        private Label labelUIQueryTimeOut;
        private TextBox textBoxQueryTimeOut;
        private Label labelQueryTimeOut;
        private TextBox textBoxMaximumRows;
        private Label labelMaximumRows;
        private Button buttonSave;
        private Button buttonCancel;
        private TextBox textBoxUIQueryTimeOut;
        private TabControl tabControlBody;
        private TabPage tabPageEditor;
        private ComboBox comboBoxFont;
        private TabPage tabPageQuery;
        private Panel panelFontSampleParent;
        private NumericUpDown numericUpDownFontSize;
        private Label labelFontAndSize;
        private GroupBox groupBoxEditorGeneral;
        private CheckBox checkBoxLineNumbers;
        private CheckBox checkBoxWordWrap;
        private GroupBox groupBoxQueryExecution;
    }
}
