using NTDLS.Katzebase.Api;
using System.Text;

namespace NTDLS.Katzebase.Management.Controls
{
    internal class DoubleBufferedDataGridView : DataGridView
    {
        private DataGridViewRow? _selectedRow;
        private DataGridViewTextBoxCell? _selectedCell;
        private readonly ContextMenuStrip _contextMenu;

        public DoubleBufferedDataGridView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = true;

            UpdateStyles();

            _contextMenu = new ContextMenuStrip();
            var copyCellValue = new ToolStripMenuItem("Copy Cell to Clipboard", null, CopyCell_Click);
            var copyRowValues = new ToolStripMenuItem("Copy Row to Clipboard", null, CopyRow_Click);
            var copyGridValues = new ToolStripMenuItem("Copy Grid Values to Clipboard", null, CopyGrid_Click);
            var copyGridValuesWithHeader = new ToolStripMenuItem("Copy Grid Values with Header to Clipboard", null, CopyGridWithHeader_Click);
            var exportToCSV = new ToolStripMenuItem("Export Grid to CSV", null, ExportGridToCSV_Click);

            _contextMenu.Items.AddRange([copyCellValue, copyRowValues, new ToolStripSeparator(),
                copyGridValues, copyGridValuesWithHeader, new ToolStripSeparator(), exportToCSV]);

            MouseUp += DataGridView_MouseUp;
        }

        private void DataGridView_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            try
            {
                _selectedRow = null;
                _selectedCell = null;

                var hitTest = HitTest(e.X, e.Y);

                if (hitTest.RowIndex >= 0 && hitTest.ColumnIndex >= 0)
                {
                    _selectedRow = Rows[hitTest.RowIndex];
                    _selectedCell = _selectedRow.Cells[hitTest.ColumnIndex] as DataGridViewTextBoxCell;
                    if (_selectedCell != null)
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
                if (_selectedCell != null)
                {
                    Clipboard.SetText(_selectedCell.Value?.ToString() ?? string.Empty);
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
                if (_selectedRow != null)
                {
                    var sb = new StringBuilder();
                    foreach (DataGridViewTextBoxCell cell in _selectedRow.Cells)
                    {
                        sb.Append(cell.Value?.ToString() ?? string.Empty).Append('\t');
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
                foreach (DataGridViewRow row in Rows)
                {
                    foreach (DataGridViewTextBoxCell cell in row.Cells)
                    {
                        sb.Append(cell.Value?.ToString()).Append('\t');
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

                foreach (DataGridViewColumn column in Columns)
                {
                    sb.Append(column.Name).Append("\t");
                }
                sb.AppendLine();

                foreach (DataGridViewRow row in Rows)
                {
                    foreach (DataGridViewTextBoxCell cell in row.Cells)
                    {
                        sb.Append(cell.Value?.ToString()).Append("\t");
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
                foreach (DataGridViewColumn column in Columns)
                {
                    sb.Append(EscapeCsvValue(column.Name)).Append(",");
                }
                sb.Length--;  // Remove the last extra comma
                sb.AppendLine();

                // Write row data
                foreach (DataGridViewRow row in Rows)
                {
                    foreach (DataGridViewTextBoxCell cell in row.Cells)
                    {
                        sb.Append(EscapeCsvValue(cell.Value?.ToString())).Append(",");
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
        private string EscapeCsvValue(string? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value.Contains('\"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            {
                // Escape double quotes by doubling them, and wrap the value in double quotes
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
