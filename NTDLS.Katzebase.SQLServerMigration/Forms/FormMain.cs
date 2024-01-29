using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.SQLServerMigration.Classes;
using System.Data.SqlClient;
using System.Dynamic;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public partial class FormMain : Form
    {
        private SQLConnectionDetails _connectionDetails = new();

        int _widthToRight = 0;
        int _heightToBottom = 0;

        public FormMain()
        {
            InitializeComponent();

            Shown += FormMain_Shown;

            _widthToRight = Width - listViewSQLServer.Width;
            _heightToBottom = Height - listViewSQLServer.Height;
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
            using (var form = new FormSQLConnect())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _connectionDetails = form.ConnectionDetails;
                    textBoxServerSchema.Text = _connectionDetails.DatabaseName;
                    PopulateTables();
                    return true;
                }
            }
            return false;
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxServerHost.Text))
            {
                return;
            }

            if (int.TryParse(textBoxServerPort.Text, out var serverPort))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textBoxServerSchema.Text))
            {
                return;
            }

            var param = new OuterWorkloadThreadParam(textBoxServerHost.Text, serverPort, textBoxServerSchema.Text);

            foreach (ListViewItem item in listViewSQLServer.Items)
            {
                UpdateListviewText(item, 2, "");
                if (item.Checked)
                {
                    param.Items.Add(new SelectedSqlItem(item, item.SubItems[0].Text, item.SubItems[1].Text));
                }
            }

            (new Thread(WorkloadThreadProc)).Start(param);

            var result = FormProgress.Singleton.ShowNew("Please wait...");
            if (result == DialogResult.OK)
            {
                MessageBox.Show("Complete!", "SQLServer Migration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var message = "An error occured while exporting data.";

                if (FormProgress.Singleton.Form.UserData != null)
                {
                    message += $"\r\n{FormProgress.Singleton.Form.UserData}";
                }

                MessageBox.Show(message, "SQLServer Migration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int _activeTableWorkers = 0;
        private int _maxTableWorkers = Environment.ProcessorCount;

        public void WorkloadThreadProc(object? p)
        {
            try
            {
                if (p == null) return;
                var param = (OuterWorkloadThreadParam)p;

                _totalRowCount = 0;

                FormProgress.Singleton.WaitForVisible();
                FormProgress.Singleton.Form.SetCanCancel(true);

                //FormProgress.Singleton.Form.SetHeaderText($"Server: {_connectionDetails.ServerName}.");
                //FormProgress.Singleton.Form.SetBodyText($"Table: [{_connectionDetails.DatabaseName}]...");
                FormProgress.Singleton.Form.SetProgressMaximum(param.Items.Count);

                foreach (var item in param.Items)
                {
                    while (_activeTableWorkers >= _maxTableWorkers)
                    {
                        Thread.Sleep(100);
                    }

                    if (FormProgress.Singleton.Form.IsCancelPending)
                    {
                        break;
                    }

                    if (_activeTableWorkers < _maxTableWorkers)
                    {
                        Interlocked.Increment(ref _activeTableWorkers);

                        var tableWorkerParam = new TabelWorkerThreadParam(param.TargetServerHost, param.TargetServerPort, param.TargetServerSchema, item);

                        (new Thread(TableWorkerThreadProc)).Start(tableWorkerParam);
                    }

                    FormProgress.Singleton.Form.IncrementProgressValue();
                }

                while (_activeTableWorkers > 0)
                {
                    Thread.Sleep(100);
                }

                FormProgress.Singleton.Close(DialogResult.OK);
            }
            catch (Exception ex)
            {
                FormProgress.Singleton.Form.UserData = ex.Message;
                FormProgress.Singleton.Close(DialogResult.Cancel);
            }
        }

        private void MoveListViewItemFirst(ListViewItem item)
        {
            if (listViewSQLServer.InvokeRequired)
            {
                listViewSQLServer.Invoke(new Action(() => MoveListViewItemFirst(item)));
                return;
            }
            listViewSQLServer.Items.Remove(item);
            listViewSQLServer.Items.Insert(0, item);
        }

        private void MoveListViewItemLast(ListViewItem item)
        {
            if (listViewSQLServer.InvokeRequired)
            {
                listViewSQLServer.Invoke(new Action(() => MoveListViewItemLast(item)));
                return;
            }
            listViewSQLServer.Items.Remove(item);
            listViewSQLServer.Items.Insert(listViewSQLServer.Items.Count - 1, item);
        }

        private void UpdateListviewText(ListViewItem item, int columnIndex, string text)
        {
            if (listViewSQLServer.InvokeRequired)
            {
                listViewSQLServer.Invoke(new Action(() => UpdateListviewText(item, columnIndex, text)));
                return;
            }
            item.SubItems[columnIndex].Text = text;
        }

        private void TableWorkerThreadProc(object? p)
        {
            if (p == null) return;
            var param = (TabelWorkerThreadParam)p;

            Thread.CurrentThread.Name = $"Import:{param.Item.Schema}:{param.Item.Table}";

            try
            {
                MoveListViewItemFirst(param.Item.ListItem);
                UpdateListviewText(param.Item.ListItem, 2, "Starting");
                ExportSQLServerTableToKatzebase(param.Item, param.TargetServerHost, param.TargetServerPort, param.TargetServerSchema);

                if (FormProgress.Singleton.Form.IsCancelPending)
                {
                    UpdateListviewText(param.Item.ListItem, 2, "Cancelled");
                }
                else UpdateListviewText(param.Item.ListItem, 2, "Complete");
            }
            catch (Exception ex)
            {
                UpdateListviewText(param.Item.ListItem, 2, "Exception");
            }
            finally
            {
                Interlocked.Decrement(ref _activeTableWorkers);
            }

            MoveListViewItemLast(param.Item.ListItem);
        }

        private long _totalRowCount = 0;
        private object _totalRowCountLock = new();

        private void ExportSQLServerTableToKatzebase(SelectedSqlItem item, string targetServerHost, int targetServerPort, string targetSchema)
        {
            int rowsPerTransaction = 10000;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                rowsPerTransaction = 100;
            }

            using var client = new KbClient(targetServerHost, targetServerPort);

            string fullTargetSchema = $"{targetSchema}:{item.Schema}:{item.Table}".Replace(":dbo", "");

            while (true)
            {
                if (FormProgress.Singleton.Form.IsCancelPending)
                {
                    break;
                }

                try
                {
                    client.Schema.CreateRecursive(fullTargetSchema);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Deadlock exception"))
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    Console.WriteLine(ex.Message);
                }
                break;
            }

            if (FormProgress.Singleton.Form.IsCancelPending)
            {
                return;
            }

            client.Transaction.Begin();

            using (var connection = new SqlConnection(_connectionDetails.ConnectionBuilder.ToString()))
            {
                connection.Open();

                try
                {
                    using (var command = new SqlCommand($"SELECT * FROM [{item.Schema}].[{item.Table}]", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (var dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int rowCount = 0;

                            while (dataReader.Read())
                            {
                                if (FormProgress.Singleton.Form.IsCancelPending)
                                {
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
                                    if (FormProgress.Singleton.Form.IsCancelPending)
                                    {
                                        break;
                                    }

                                    try
                                    {
                                        client.Document.Store(fullTargetSchema, new KbDocument(dbObject));
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex.Message.Contains("Deadlock exception"))
                                        {
                                            Thread.Sleep(500);
                                            client.Transaction.Begin();
                                            continue;
                                        }
                                        Console.WriteLine(ex.Message);
                                    }
                                    break;
                                }

                                lock (_totalRowCountLock)
                                {
                                    if (_totalRowCount % 100 == 0)
                                    {
                                        FormProgress.Singleton.Form.SetBodyText($"Total rows processed: {_totalRowCount:n0}...");
                                    }

                                    _totalRowCount++;
                                }

                                if (rowCount > 0 && rowCount % 100 == 0)
                                {
                                    UpdateListviewText(item.ListItem, 2, $"Rows {rowCount:n0}");
                                }

                                rowCount++;
                            }
                        }
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    client.Transaction.Rollback();
                    throw;
                }

                client.Transaction.Commit();
            }
        }

        private void PopulateTables()
        {
            listViewSQLServer.Items.Clear();

            using var connection = new SqlConnection(_connectionDetails.ConnectionBuilder.ToString());
            try
            {
                connection.Open();

                string text = "SELECT OBJECT_SCHEMA_NAME(object_id) as [Schema], name as [Name]"
                    + " FROM sys.tables WHERE type = 'u' ORDER BY OBJECT_SCHEMA_NAME(object_id) + '.' + name";

                using var command = new SqlCommand(text, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var item = new ListViewItem($"{reader[0]}");
                    item.SubItems.Add($"{reader[1]}");
                    item.SubItems.Add($"");
                    item.Checked = true;

                    listViewSQLServer.Items.Add(item);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                connection.Close();
            }
        }

        private void changeConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeConnection();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new FormAbout();
            form.ShowDialog();
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            listViewSQLServer.Width = Width - _widthToRight;
            listViewSQLServer.Height = Height - _heightToBottom;
            buttonImport.Left = listViewSQLServer.Right - buttonImport.Width;
        }
    }
}
