using Dapper;
using NTDLS.Helpers;
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.SQLServerMigration.Classes;
using NTDLS.Katzebase.SQLServerMigration.Properties;
using NTDLS.WinFormsHelpers;
using Microsoft.Data.SqlClient;
using System.Dynamic;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public partial class FormMain : Form
    {
        private SQLConnectionDetails _connectionDetails = new();

        private double _targetPageSizeBytes = 1024.0 * 100.0;
        private int _activeTableWorkers;
        private readonly int _maxTableWorkers = Environment.ProcessorCount;
        private readonly int _widthToRight;
        private readonly int _heightToBottom;
        private bool _isCancelPending = false;

        public FormMain()
        {
            InitializeComponent();

            Shown += FormMain_Shown;

            _widthToRight = Width - dataGridViewSqlServer.Width;
            _heightToBottom = Height - dataGridViewSqlServer.Height;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            dataGridViewSqlServer.AllowUserToAddRows = false;
            dataGridViewSqlServer.AllowUserToDeleteRows = false;
            dataGridViewSqlServer.AllowUserToOrderColumns = false;
            dataGridViewSqlServer.AllowUserToResizeRows = false;

            dataGridViewSqlServer.CellValidating += DataGridViewSqlServer_CellValidating;

        }

        private void DataGridViewSqlServer_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == ColumnPageSize.Index)
            {
                if (int.TryParse(e.FormattedValue?.ToString(), out int result) && result > 0 && result < int.MaxValue)
                {
                    dataGridViewSqlServer.Rows[e.RowIndex].ErrorText = string.Empty;
                }
                else
                {
                    e.Cancel = true;
                    dataGridViewSqlServer.Rows[e.RowIndex].ErrorText = $"Page Size must be a valid integer between 1 and {int.MaxValue:n0}.";
                }
            }
            else if (e.ColumnIndex == ColumnTargetSchema.Index)
            {
                if (string.IsNullOrWhiteSpace(e.FormattedValue?.ToString()) == false)
                {
                    dataGridViewSqlServer.Rows[e.RowIndex].ErrorText = string.Empty;
                }
                else
                {
                    e.Cancel = true;
                    dataGridViewSqlServer.Rows[e.RowIndex].ErrorText = $"Target Schema must contain a valid schema name.";
                }
            }
        }

        private void FormMain_Shown(object? sender, EventArgs e)
        {
            if (ChangeConnection() == false)
            {
                Close();
            }
        }

        private bool ChangeConnection()
        {
            using var form = new FormSQLConnect();
            if (form.ShowDialog() == DialogResult.OK)
            {
                _connectionDetails = form.ConnectionDetails;
                textBoxServerSchema.Text = _connectionDetails.DatabaseName;
                PopulateTables();
                return true;
            }
            return false;
        }

        private void ButtonImport_Click(object sender, EventArgs e)
        {
            if (buttonImport.Text.Is("Cancel"))
            {
                if (MessageBox.Show($"Cancel the import process?", "SQLServer Migration", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _isCancelPending = true;
                }

                return;
            }

            _isCancelPending = false;

            buttonImport.BackColor = Color.FromArgb(255, 192, 192);
            buttonImport.Text = "Cancel";

            if (string.IsNullOrWhiteSpace(textBoxServerHost.Text))
            {
                return;
            }

            if (!int.TryParse(textBoxServerPort.Text, out var serverPort))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textBoxServerSchema.Text))
            {
                return;
            }

            var param = new OuterWorkloadThreadParam(textBoxServerHost.Text, serverPort,
                textBoxUsername.Text, KbClient.HashPassword(textBoxPassword.Text));

            foreach (DataGridViewRow item in dataGridViewSqlServer.Rows)
            {
                UpdateDataGridViewText(item, "");

                var importData = (DataGridViewCheckBoxCell)item.Cells[ColumnImportData.Index];
                var importIndexes = (DataGridViewCheckBoxCell)item.Cells[ColumnImportIndexes.Index];

                if (importData.Value != null && (bool)importData.Value
                    || importIndexes.Value != null && (bool)importIndexes.Value)
                {
                    param.Items.Add(new SelectedImportObject(item,
                                (item.Cells[ColumnSourceTable.Index].Value?.ToString()).EnsureNotNull(),
                                (textBoxServerSchema.Text + ":" + (item.Cells[ColumnTargetSchema.Index].Value?.ToString()).EnsureNotNull()).Trim(':'),
                                int.Parse((item.Cells[ColumnPageSize.Index].Value?.ToString()).EnsureNotNull()))
                    {
                        ImportData = (importData.Value != null && (bool)importData.Value),
                        ImportIndexes = (importIndexes.Value != null && (bool)importIndexes.Value)
                    });
                }
            }

            (new Thread(WorkloadThreadProc)).Start(param);
        }

        public void WorkloadThreadProc(object? p)
        {
            try
            {
                if (p == null) return;
                var param = (OuterWorkloadThreadParam)p;

                _totalRowCount = 0;

                #region Create Schemas.

                using (var client = new KbClient(param.TargetServerHost, param.TargetServerPort, param.TargetServerUsername, param.TargetServerPasswordHash, "SQLServerMigration"))
                {
                    var alreadyCreated = new HashSet<string>();

                    var schemasToCreate = param.Items.Select(o => new
                    {
                        Name = o.TargetServerSchema,
                        PageSize = o.TargetServerSchemaPageSize
                    }).Distinct().ToList();

                    foreach (var schemaToCreate in schemasToCreate)
                    {
                        var parts = schemaToCreate.Name.Split(':');

                        //Loop though all of the schema parts looking for any items with an exact match
                        //  so we can create the schema while respecting the specified page size.
                        for (int i = 1; i < parts.Length + 1; i++)
                        {
                            var partialSchema = string.Join(':', parts.Take(i));

                            if (alreadyCreated.Contains(partialSchema, StringComparer.InvariantCultureIgnoreCase) == false)
                            {
                                var specificSchema = schemasToCreate.FirstOrDefault(o => o.Name.Equals(partialSchema, StringComparison.InvariantCulture));
                                if (specificSchema != null)
                                {
                                    if (client.Schema.Exists(specificSchema.Name) == false)
                                    {
                                        client.Schema.Create(specificSchema.Name, (uint)specificSchema.PageSize);
                                    }
                                    alreadyCreated.Add(specificSchema.Name);
                                }
                                else
                                {
                                    if (client.Schema.Exists(partialSchema) == false)
                                    {
                                        client.Schema.Create(partialSchema);
                                    }
                                    alreadyCreated.Add(partialSchema);
                                }
                            }
                        }

                        //Create the full schema name:
                        if (alreadyCreated.Contains(schemaToCreate.Name, StringComparer.InvariantCultureIgnoreCase) == false)
                        {
                            client.Schema.CreateRecursive(schemaToCreate.Name, (uint)schemaToCreate.PageSize);
                        }
                    }
                }

                #endregion

                #region Queue worker threads.

                foreach (var item in param.Items)
                {
                    while (_activeTableWorkers >= _maxTableWorkers)
                    {
                        Thread.Sleep(100);
                    }

                    if (_isCancelPending)
                    {
                        break;
                    }

                    if (_activeTableWorkers < _maxTableWorkers)
                    {
                        Interlocked.Increment(ref _activeTableWorkers);

                        var tableWorkerParam = new TableWorkerThreadParam(param.TargetServerHost,
                            param.TargetServerPort, item.TargetServerSchema, param.TargetServerUsername, param.TargetServerPasswordHash, item);

                        (new Thread(TableWorkerThreadProc)).Start(tableWorkerParam);
                    }
                }

                #endregion

                while (_activeTableWorkers > 0)
                {
                    Thread.Sleep(100);
                }

                this.InvokeMessageBox($"Complete.", "SQLServer Migration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.InvokeMessageBox($"Complete with errors: {ex.GetBaseException().Message}", "SQLServer Migration", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            finally
            {
                Invoke(new Action(() =>
                {
                    buttonImport.BackColor = Color.FromArgb(192, 255, 192);
                    buttonImport.Text = "Start";
                }));
            }
        }

        private void MoveDataGridViewRowFirst(DataGridViewRow item)
        {
            if (dataGridViewSqlServer.InvokeRequired)
            {
                dataGridViewSqlServer.Invoke(new Action(() => MoveDataGridViewRowFirst(item)));
                return;
            }
            dataGridViewSqlServer.Rows.Remove(item);
            dataGridViewSqlServer.Rows.Insert(0, item);
        }

        private void MoveDataGridViewRowLast(DataGridViewRow item)
        {
            if (dataGridViewSqlServer.InvokeRequired)
            {
                dataGridViewSqlServer.Invoke(new Action(() => MoveDataGridViewRowLast(item)));
                return;
            }
            dataGridViewSqlServer.Rows.Remove(item);
            dataGridViewSqlServer.Rows.Insert(dataGridViewSqlServer.Rows.Count - 1, item);
        }

        private void UpdateDataGridViewText(DataGridViewRow item, string text)
        {
            if (dataGridViewSqlServer.InvokeRequired)
            {
                dataGridViewSqlServer.Invoke(new Action(() => UpdateDataGridViewText(item, text)));
                return;
            }
            item.Cells[ColumnStatus.Index].Value = text;
        }

        private void MoveRowToBottom(DataGridViewRow item)
        {
            if (dataGridViewSqlServer.InvokeRequired)
            {
                dataGridViewSqlServer.Invoke(new Action(() => MoveRowToBottom(item)));
                return;
            }

            int bottomIndex = dataGridViewSqlServer.Rows.Count - 1;
            DataGridViewRow row = dataGridViewSqlServer.Rows[item.Index];
            dataGridViewSqlServer.Rows.RemoveAt(item.Index);
            dataGridViewSqlServer.Rows.Insert(bottomIndex, row);
            dataGridViewSqlServer.ClearSelection();
        }

        private void MoveRowToTop(DataGridViewRow item)
        {
            if (dataGridViewSqlServer.InvokeRequired)
            {
                dataGridViewSqlServer.Invoke(new Action(() => MoveRowToTop(item)));
                return;
            }

            DataGridViewRow row = dataGridViewSqlServer.Rows[item.Index];
            dataGridViewSqlServer.Rows.RemoveAt(item.Index);
            dataGridViewSqlServer.Rows.Insert(0, row);
            dataGridViewSqlServer.ClearSelection();
            dataGridViewSqlServer.FirstDisplayedScrollingRowIndex = 0;
        }

        private void TableWorkerThreadProc(object? p)
        {
            if (p == null) return;
            var param = (TableWorkerThreadParam)p;

            Thread.CurrentThread.Name = $"Import:{param.Item.SourceObjectName}";

            try
            {
                MoveDataGridViewRowFirst(param.Item.RowItem);
                UpdateDataGridViewText(param.Item.RowItem, "Starting");

                MoveRowToTop(param.Item.RowItem);

                ExportSQLServerTableToKatzebase(param.Item, param.TargetServerHost, param.TargetServerPort, param.Username, param.Password, param.TargetServerSchema);

                if (_isCancelPending)
                {
                    UpdateDataGridViewText(param.Item.RowItem, "Cancelled");
                }
                else
                {
                    UpdateDataGridViewText(param.Item.RowItem, "Complete");
                }

                MoveRowToBottom(param.Item.RowItem);
            }
            catch
            {
                UpdateDataGridViewText(param.Item.RowItem, "Exception");
            }
            finally
            {
                Interlocked.Decrement(ref _activeTableWorkers);
            }

            MoveDataGridViewRowLast(param.Item.RowItem);
        }

        private long _totalRowCount;
        private object _totalRowCountLock = new();

        private void ExportSQLServerTableToKatzebase(SelectedImportObject item, string targetServerHost, int targetServerPort, string username, string password, string targetSchema)
        {
            int rowsPerTransaction = 10000;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                rowsPerTransaction = 100;
            }

            using var client = new KbClient(targetServerHost, targetServerPort, username, password, "SQLServerMigration")
            {
                QueryTimeout = TimeSpan.FromDays(7)
            };

            if (_isCancelPending)
            {
                return;
            }

            using (var connection = new SqlConnection(_connectionDetails.ConnectionBuilder.ToString()))
            {
                connection.Open();

                #region Import Data.

                if (item.ImportData)
                {
                    client.Transaction.Begin();
                    try
                    {
                        using (var command = new SqlCommand($"SELECT * FROM {item.SourceObjectName}", connection))
                        {
                            command.CommandTimeout = 10000;
                            command.CommandType = System.Data.CommandType.Text;

                            using (var dataReader = command.ExecuteReader())
                            {
                                int rowCount = 0;

                                while (dataReader.Read())
                                {
                                    if (_isCancelPending)
                                    {
                                        dataReader.Close();
                                        break;
                                    }

                                    var dbObject = new ExpandoObject() as IDictionary<string, object>;

                                    for (int iField = 0; iField < dataReader.FieldCount; iField++)
                                    {
                                        var dataType = dataReader.GetFieldType(iField);
                                        if (dataType != null)
                                        {
                                            dbObject.Add(dataReader.GetName(iField), dataReader[iField]?.ToString()?.Trim() ?? "");
                                        }
                                    }

                                    if (rowCount > 0 && (rowCount % rowsPerTransaction) == 0)
                                    {
                                        client.Transaction.Commit();
                                        client.Transaction.Begin();
                                    }

                                    while (true)
                                    {
                                        if (_isCancelPending)
                                        {
                                            break;
                                        }

                                        try
                                        {
                                            client.Document.Store(targetSchema, new KbDocument(dbObject));
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.Message.Contains("Deadlock exception"))
                                            {
                                                Thread.Sleep(500);
                                                client.Transaction.Begin();
                                                continue;
                                            }
                                        }
                                        break;
                                    }

                                    lock (_totalRowCountLock)
                                    {
                                        //if (_totalRowCount % 100 == 0)
                                        //{
                                        //FormProgress.Singleton.Form.SetBodyText($"Total rows processed: {_totalRowCount:n0}...");
                                        //}

                                        _totalRowCount++;
                                    }

                                    if (rowCount > 0 && rowCount % 100 == 0)
                                    {
                                        UpdateDataGridViewText(item.RowItem, $"Rows {rowCount:n0}");
                                    }

                                    rowCount++;
                                }
                            }
                        }
                        connection.Close();
                    }
                    catch
                    {
                        client.Transaction.Rollback();
                        throw;
                    }
                }

                #endregion

                if (_isCancelPending)
                {
                    return;
                }

                #region Import Inexes.

                if (item.ImportIndexes)
                {
                    var sourceIndexes = connection.Query<ObjectSourceIndex>(Resources.SqlGetObjectIndexes, new
                    {
                        ObjectName = item.SourceObjectName
                    }).GroupBy(o => o.IndexName).ToList();

                    foreach (var sourceIndex in sourceIndexes)
                    {
                        if (_isCancelPending)
                        {
                            return;
                        }

                        if (client.Schema.Indexes.Exists(item.TargetServerSchema, sourceIndex.Key.EnsureNotNull()) == false)
                        {
                            var targetIndex = new KbIndex(sourceIndex.Key.EnsureNotNull())
                            {
                                IsUnique = (sourceIndex.First().IsUnique == true)
                            };
                            foreach (var column in sourceIndex)
                            {
                                targetIndex.AddAttribute(column.ColumnName.EnsureNotNull());
                            }

                            UpdateDataGridViewText(item.RowItem, $"Index: {sourceIndex.Key}");

                            client.Schema.Indexes.Create(item.TargetServerSchema, targetIndex);
                        }
                    }
                }

                #endregion

                client.Transaction.Commit();
            }
        }

        private void PopulateTables()
        {
            dataGridViewSqlServer.Rows.Clear();

            using var connection = new SqlConnection(_connectionDetails.ConnectionBuilder.ToString());
            try
            {
                connection.Open();

                var sourceObjects = connection.Query<ObjectSourceObject>(Resources.SqlGetObjectsAndSizes, new
                {
                    TargetPageSizeBytes = _targetPageSizeBytes
                });

                foreach (var sourceObject in sourceObjects)
                {
                    string analysis = $"Rows: {sourceObject.TotalRows:n0}, Avg. Size: {Formatters.FileSize(sourceObject.AvgRowSizeBytes)}";

                    var targetSchemaObject = sourceObject.TargetSchemaObject.EnsureNotNull();

                    if (targetSchemaObject.StartsWith("dbo:", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        targetSchemaObject = sourceObject.TargetObject;
                    }

                    dataGridViewSqlServer.Rows.Add(true, true, sourceObject.SourceSchemaObject, analysis, targetSchemaObject, sourceObject.TargetPageSize, string.Empty);
                }

                // Auto-size all columns based on content (but only once)
                dataGridViewSqlServer.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                // Set the last column to fill the remaining space
                dataGridViewSqlServer.Columns[dataGridViewSqlServer.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                // Allow user to resize the columns manually after the initial auto-resizing
                dataGridViewSqlServer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            }
            catch
            {
            }
            finally
            {
                connection.Close();
            }
        }

        private void ChangeConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeConnection();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new FormAbout();
            form.ShowDialog();
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            dataGridViewSqlServer.Width = Width - _widthToRight;
            dataGridViewSqlServer.Height = Height - _heightToBottom;
            buttonImport.Left = dataGridViewSqlServer.Right - buttonImport.Width;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (buttonImport.Text.Is("Cancel"))
            {
                if (MessageBox.Show($"Cancel the import process?", "SQLServer Migration", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _isCancelPending = true;
                }

                e.Cancel = true;
                return;
            }
        }
    }
}
