using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NTDLS.Katzebase.Management.Controls;
using NTDLS.Katzebase.Management.Properties;
using NTDLS.Persistence;
using NTDLS.WinFormsHelpers;
using System.Xml;

namespace NTDLS.Katzebase.Management
{
    public partial class FormSettings : Form
    {
        private readonly TextEditor _fontSampleTextbox;
        private readonly Graphics _graphics;

        public FormSettings()
        {
            InitializeComponent();

            _graphics = CreateGraphics();

            AcceptButton = buttonSave;
            CancelButton = buttonCancel;

            textBoxUIQueryTimeOut.Text = $"{Program.Settings.UIQueryTimeOut:n0}";
            textBoxQueryTimeOut.Text = $"{Program.Settings.UserQueryTimeOut:n0}";
            textBoxMaximumRows.Text = $"{Program.Settings.QueryMaximumRows:n0}";
            checkBoxLineNumbers.Checked = Program.Settings.EditorShowLineNumbers;
            checkBoxWordWrap.Checked = Program.Settings.EditorWordWrap;

            _fontSampleTextbox = new TextEditor
            {
                Text = "SELECT\r\n\t11 ^ (2 + 1) + 'ten' + (Length('A10CharStr') * 10 + 2),\r\n\t10 + 10 + (11 ^ 3) + 10 + '->' + Guid(),\r\n\t10 + 10 + 'ten' + 10 * 10,\r\n\t'ten (' + 10 * 10 + ') : ' + DateTimeUTC('yyyy/MM/dd hh:mm:ss tt')\r\nFROM\r\n\tSingle\r\n"
            };

            FullyFeaturedCodeEditor.ApplyEditorSettings(_fontSampleTextbox);

            panelFontSampleParent.Controls.Add(new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = _fontSampleTextbox
            });

            using var stringReader = new StringReader(Resources.Highlighter);
            using var reader = XmlReader.Create(stringReader);
            _fontSampleTextbox.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            reader.Close();
            stringReader.Close();

            foreach (var font in FontFamily.Families)
            {
                if (IsMonospacedFont(font))
                {
                    comboBoxFont.Items.Add(font.Name);
                }
            }
            comboBoxFont.Text = Program.Settings.EditorFontFamily;
            numericUpDownFontSize.Value = (decimal)Program.Settings.EditorFontSize;

            numericUpDownFontSize.ValueChanged += (object? sender, EventArgs e) => UpdateFontSample();
            comboBoxFont.SelectedIndexChanged += (object? sender, EventArgs e) => UpdateFontSample();
            checkBoxLineNumbers.CheckedChanged += (object? sender, EventArgs e) => UpdateFontSample();
            checkBoxWordWrap.CheckedChanged += (object? sender, EventArgs e) => UpdateFontSample();

            Disposed += (object? sender, EventArgs e) => _graphics.Dispose();

            UpdateFontSample();
        }

        private bool IsMonospacedFont(FontFamily fontFamily)
        {
            using var font = new Font(fontFamily, 12);
            return _graphics.MeasureString("i", font).Width == _graphics.MeasureString("W", font).Width;
        }

        private void UpdateFontSample()
        {
            var selectedFontName = comboBoxFont.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedFontName) == false)
            {
                try
                {
                    var fontSize = numericUpDownFontSize.Value;
                    if (fontSize > 0)
                    {
                        _fontSampleTextbox.WordWrap = checkBoxWordWrap.Checked;
                        _fontSampleTextbox.ShowLineNumbers = checkBoxLineNumbers.Checked;
                        _fontSampleTextbox.FontFamily = new System.Windows.Media.FontFamily(selectedFontName);
                        _fontSampleTextbox.FontSize = (double)fontSize;
                    }
                }
                catch { }
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = new ManagementSettings()
                {
                    UIQueryTimeOut = textBoxUIQueryTimeOut.GetAndValidateNumeric(1, 600, "UI query time-out must be between [min] and [max]."),
                    UserQueryTimeOut = textBoxQueryTimeOut.GetAndValidateNumeric(-1, 86400, "Query time-out must be between [min] (infinite) and [max]."),
                    QueryMaximumRows = textBoxMaximumRows.GetAndValidateNumeric(-1, int.MaxValue, "Maximum rows must be between [min] (no maximum) and [max]."),
                    EditorFontSize = (double)numericUpDownFontSize.Value,
                    EditorShowLineNumbers = checkBoxLineNumbers.Checked,
                    EditorWordWrap = checkBoxWordWrap.Checked
                };

                LocalUserApplicationData.SaveToDisk($"{Api.KbConstants.FriendlyName}\\Management", settings);
                Program.Settings = settings;

                this.InvokeClose(DialogResult.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Api.KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.InvokeClose(DialogResult.Cancel);
        }
    }
}
