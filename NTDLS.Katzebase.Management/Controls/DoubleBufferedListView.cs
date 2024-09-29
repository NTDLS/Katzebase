using NTDLS.Katzebase.Client;
using System.Text;

namespace NTDLS.Katzebase.Management.Controls
{
    internal class DoubleBufferedListView : ListView
    {
        private ListViewItem? _selectedItem;
        private ListViewItem.ListViewSubItem? _selectedSubItem;
        private readonly ContextMenuStrip _contextMenu;

        public DoubleBufferedListView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
            GridLines = true;
            FullRowSelect = false;
            View = View.Details;

            _contextMenu = new ContextMenuStrip();
            var copyCellValue = new ToolStripMenuItem("Copy Cell to Clipboard", null, CopyCell_Click);
            var copyRowValues = new ToolStripMenuItem("Copy Row to Clipboard", null, CopyRow_Click);
            var copyGridValues = new ToolStripMenuItem("Copy Grid Values to Clipboard", null, CopyGrid_Click);
            var copyGridValuesWithHeader = new ToolStripMenuItem("Copy Grid Values with Header to Clipboard", null, CopyGridWithHeader_Click);
            var exportToCSV = new ToolStripMenuItem("Export Grid to CSV", null, ExportGridToCSV_Click);

            _contextMenu.Items.AddRange([copyCellValue, copyRowValues, new ToolStripSeparator(),
                copyGridValues, copyGridValuesWithHeader, new ToolStripSeparator(), exportToCSV]);

            MouseUp += ListView_MouseUp;
        }

        private void ListView_MouseUp(object? sender, MouseEventArgs e)
        {
            try
            {
                _selectedItem = null;
                _selectedSubItem = null;

                var hitTest = HitTest(e.Location);
                if (hitTest.Item != null && hitTest.SubItem != null)
                {
                    _selectedItem = hitTest.Item;
                    _selectedSubItem = hitTest.SubItem;

                    if (hitTest.Item != null)
                    {
                        _contextMenu.Show(this, e.Location);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Event handler to copy the selected cell content to the clipboard.
        /// </summary>
        private void CopyCell_Click(object? sender, EventArgs? e)
        {
            try
            {
                if (_selectedSubItem != null)
                {
                    Clipboard.SetText(_selectedSubItem.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred coping the cell to the clipboard: {ex.Message}",
                    KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Event handler to copy the entire selected row content to the clipboard.
        /// </summary>
        private void CopyRow_Click(object? sender, EventArgs? e)
        {
            try
            {
                if (_selectedItem != null)
                {
                    var sb = new StringBuilder();
                    foreach (ListViewItem.ListViewSubItem subItem in _selectedItem.SubItems)
                    {
                        sb.Append(subItem.Text).Append('\t');
                    }
                    Clipboard.SetText(sb.ToString().TrimEnd('\t'));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred coping the cell to the clipboard: {ex.Message}",
                    KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Event handler to copy the entire grid content to the clipboard.
        /// </summary>
        private void CopyGrid_Click(object? sender, EventArgs? e)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (ListViewItem item in Items)
                {
                    foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                    {
                        sb.Append(subItem.Text).Append("\t");
                    }
                    sb.AppendLine();
                }

                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred coping the cell to the clipboard: {ex.Message}",
                    KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Event handler to copy the entire grid's content to the clipboard
        /// </summary>
        private void CopyGridWithHeader_Click(object? sender, EventArgs? e)
        {
            try
            {
                var sb = new StringBuilder();

                foreach (ColumnHeader column in Columns)
                {
                    sb.Append(column.Text).Append("\t");
                }
                sb.AppendLine();

                foreach (ListViewItem item in Items)
                {
                    foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                    {
                        sb.Append(subItem.Text).Append("\t");
                    }
                    sb.AppendLine();
                }

                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred coping the cell to the clipboard: {ex.Message}",
                    KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Event handler to copy the entire grid's content to the a CSV file.
        /// </summary>
        private void ExportGridToCSV_Click(object? sender, EventArgs? e)
        {
            try
            {
                string? csvFilePath = null;

                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = $"Comma separated file (*.csv)|*.csv|Text file (*.txt)|*.txt|All files (*.*)|*.*";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        csvFilePath = sfd.FileName;
                    }
                    else
                    {
                        return;
                    }
                }

                var sb = new StringBuilder();

                // Write column headers
                foreach (ColumnHeader column in Columns)
                {
                    sb.Append(EscapeCsvValue(column.Text)).Append(",");
                }
                sb.Length--;  // Remove the last extra comma
                sb.AppendLine();

                // Write row data
                foreach (ListViewItem item in Items)
                {
                    foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                    {
                        sb.Append(EscapeCsvValue(subItem.Text)).Append(",");
                    }
                    sb.Length--;  // Remove the last extra comma
                    sb.AppendLine();
                }

                File.WriteAllText(csvFilePath, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred exporting the grid data: {ex.Message}",
                    KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Helper function to escape values for CSV format
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string EscapeCsvValue(string value)
        {
            if (value.Contains('\"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            {
                // Escape double quotes by doubling them, and wrap the value in double quotes
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
