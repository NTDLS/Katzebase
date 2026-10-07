using Dapper;
using Microsoft.Data.SqlClient;
using NTDLS.Helpers;
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.SQLServerMigration.Classes;
using NTDLS.Katzebase.SQLServerMigration.Properties;
using NTDLS.WinFormsHelpers;
using System.Dynamic;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public partial class FormMain : Form
    {
        private SQLConnectionDetails _connectionDetails = new();

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
            if (e.ColumnIndex == ColumnTargetSchema.Index)
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
                                (textBoxServerSchema.Text + ":" + (item.Cells[ColumnTargetSchema.Index].Value?.ToString()).EnsureNotNull()).Trim(':'))
                    {
                        ImportData = (importData.Value != null && (bool)importData.Value),
                        ImportIndexes = (importIndexes.Value != null && (bool)importIndexes.Value)
                    });
                }
            }

            (new Thread(WorkloadThreadProc) { IsBackground = true }).Start(param);
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
                                        client.Schema.Create(specificSchema.Name);
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
                            client.Schema.CreateRecursive(schemaToCreate.Name);
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

                        (new Thread(TableWorkerThreadProc) { IsBackground = true }).Start(tableWorkerParam);
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
            catch (Exception ex)
            {
                UpdateDataGridViewText(param.Item.RowItem, $"Exception: {ex.GetBaseException().Message}");
            }
            finally
            {
                Interlocked.Decrement(ref _activeTableWorkers);
            }

            MoveDataGridViewRowLast(param.Item.RowItem);
        }

        private long _totalRowCount;

        /// <summary>
        /// The maximum number of rows sent to the server in a single Document.StoreMany() call.
        /// </summary>
        private const int rowsPerBatch = 1000;

        /// <summary>
        /// Batches are also sent early once their (approximate) serialized size reaches this, so that wide rows don't produce huge messages.
        /// </summary>
        private const long maxBatchBytes = 4 * 1024 * 1024;

        /// <summary>
        /// How many times a batch is retried when it is chosen as a deadlock victim before the import of the table is failed.
        /// </summary>
        private const int maxDeadlockRetries = 10;

        /// <summary>
        /// Stores a batch of documents. The batch is atomic on the server, so when it is chosen as a deadlock
        /// victim none of it was stored and the whole batch can safely be sent again. Any other error is thrown
        /// to the caller rather than being swallowed (which previously silently dropped rows).
        /// </summary>
        private static void StoreBatch(KbClient client, string targetSchema, List<KbDocument> batch)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    client.Document.StoreMany(targetSchema, batch);
                    return;
                }
                catch (Exception ex) when (attempt < maxDeadlockRetries && IsDeadlock(ex))
                {
                    Thread.Sleep(Random.Shared.Next(50, 250) * attempt);
                }
            }
        }

        private static bool IsDeadlock(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is KbDeadlockException || current.Message.Contains("Deadlock", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void ExportSQLServerTableToKatzebase(SelectedImportObject item, string targetServerHost, int targetServerPort, string username, string password, string targetSchema)
        {
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
                    //Rows are sent to the server in batches using Document.StoreMany(): one round trip per batch rather than per row.
                    //  Each batch is stored atomically in its own transaction, so if a batch fails (e.g. it is chosen as a deadlock
                    //  victim) exactly that batch is rolled back and can be safely re-sent. No explicit transaction is used, because
                    //  a rollback of an explicit transaction would also discard all of the previously sent batches.
                    var batch = new List<KbDocument>(rowsPerBatch);
                    long batchBytes = 0;
                    long rowCount = 0;

                    using (var command = new SqlCommand($"SELECT * FROM {item.SourceObjectName}", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (var dataReader = command.ExecuteReader())
                        {
                            var fieldNames = Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToArray();

                            while (dataReader.Read())
                            {
                                if (_isCancelPending)
                                {
                                    return;
                                }

                                var dbObject = new Dictionary<string, string>(fieldNames.Length);
                                for (int iField = 0; iField < fieldNames.Length; iField++)
                                {
                                    dbObject[fieldNames[iField]] = dataReader[iField]?.ToString()?.Trim() ?? "";
                                }

                                var document = new KbDocument(dbObject);
                                batch.Add(document);
                                batchBytes += document.Content.Length;

                                if (batch.Count >= rowsPerBatch || batchBytes >= maxBatchBytes)
                                {
                                    StoreBatch(client, targetSchema, batch);
                                    rowCount += batch.Count;
                                    Interlocked.Add(ref _totalRowCount, batch.Count);
                                    batch.Clear();
                                    batchBytes = 0;

                                    UpdateDataGridViewText(item.RowItem, $"Rows {rowCount:n0}");
                                }
                            }
                        }
                    }

                    if (batch.Count > 0 && _isCancelPending == false)
                    {
                        StoreBatch(client, targetSchema, batch);
                        rowCount += batch.Count;
                        Interlocked.Add(ref _totalRowCount, batch.Count);
                        UpdateDataGridViewText(item.RowItem, $"Rows {rowCount:n0}");
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

                            if (targetIndex.Attributes.Count > 0)
                            {
                                UpdateDataGridViewText(item.RowItem, $"Index: {sourceIndex.Key}");
                                client.Schema.Indexes.Create(item.TargetServerSchema, targetIndex);
                            }
                        }
                    }
                }

                #endregion
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
                });

                foreach (var sourceObject in sourceObjects)
                {
                    if (sourceObject != null)
                    {
                        string analysis = $"Rows: {sourceObject.TotalRows:n0}, Avg. Size: {Formatters.FileSize(sourceObject.AvgRowSizeBytes)}";

                        var targetSchemaObject = sourceObject.TargetSchemaObject.EnsureNotNull();

                        if (targetSchemaObject.StartsWith("dbo:", StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            targetSchemaObject = sourceObject.TargetObject;
                        }

                        dataGridViewSqlServer.Rows.Add(true, true, sourceObject.SourceSchemaObject ?? string.Empty, analysis, targetSchemaObject ?? string.Empty, string.Empty);
                    }
                }

                if (dataGridViewSqlServer.Columns.Count > 0)
                {
                    // Auto-size all columns based on content (but only once)
                    dataGridViewSqlServer.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                    // Set the last column to fill the remaining space
                    dataGridViewSqlServer.Columns[dataGridViewSqlServer.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                    // Allow user to resize the columns manually after the initial auto-resizing
                    dataGridViewSqlServer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                }
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
