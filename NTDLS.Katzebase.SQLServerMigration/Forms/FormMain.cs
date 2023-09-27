using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;
using System.Data.SqlClient;
using System.Dynamic;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public partial class FormMain : Form
    {
        private SQLConnectionDetails _connectionDetails = new();

        public FormMain()
        {
            InitializeComponent();

            this.Shown += FormMain_Shown;
        }

        private void FormMain_Shown(object? sender, EventArgs e)
        {
            using (var form = new FormSQLConnect())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _connectionDetails = form.ConnectionDetails;

                    textBoxBKServerSchema.Text = _connectionDetails.DatabaseName;
                    PopulateTables();
                }
                else
                {
                    Close();
                }
            }
        }

        class WorkloadThreadParam
        {
            public class SelectedSqlItem
            {
                public string Schema { get; set; }
                public string Table { get; set; }

                public SelectedSqlItem(string schema, string name)
                {
                    Schema = schema;
                    Table = name;
                }
            }

            public List<SelectedSqlItem> Items { get; set; } = new();
            public string TargetServerAddress { get; set; }
            public string TargetServerSchema { get; set; }

            public WorkloadThreadParam(string targetServerAddress, string targetServerSchema)
            {
                TargetServerAddress = targetServerAddress;
                TargetServerSchema = targetServerSchema;
            }
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            var param = new WorkloadThreadParam(textBoxBKServerAddress.Text, textBoxBKServerSchema.Text);

            foreach (ListViewItem item in listViewSQLServer.Items)
            {
                if (item.Checked)
                {
                    param.Items.Add(new WorkloadThreadParam.SelectedSqlItem(item.SubItems[0].Text, item.SubItems[1].Text));
                }
            }

            (new Thread(WorkloadThreadProc)).Start(param);

            FormProgress.Singleton.ShowNew("Please wait...");
        }

        public void WorkloadThreadProc(object? p)
        {
            if (p == null) return;
            var param = (WorkloadThreadParam)p;

            FormProgress.Singleton.WaitForVisible();

            FormProgress.Singleton.Form.SetHeaderText($"Server: {_connectionDetails.ServerName}.");
            FormProgress.Singleton.Form.SetBodyText($"Table: [{_connectionDetails.DatabaseName}]...");

            FormProgress.Singleton.Form.SetProgressMaximum(param.Items.Count);

            foreach (var item in param.Items)
            {
                FormProgress.Singleton.Form.SetBodyText($"Processing: [{item.Schema}].[{item.Table}]...");
                ExportSQLServerTableToKatzebase(item.Schema, item.Table, param.TargetServerAddress, param.TargetServerSchema);
                FormProgress.Singleton.Form.IncrementProgressValue();
            }
        }

        private void PopulateTables()
        {
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

        public void ExportSQLServerTableToKatzebase(string sourceSchema, string sourceTable, string targetServerAddress, string targetSchema)
        {
            int rowsPerTransaction = 10000;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                rowsPerTransaction = 100;
            }

            using var client = new KbClient(targetServerAddress);

            string fullTargetSchema = $"{targetSchema}:{sourceSchema}:{sourceTable}".Replace(":dbo", "");

            client.Schema.CreateFullSchema(fullTargetSchema);

            client.Transaction.Begin();

            using (var connection = new SqlConnection(_connectionDetails.ConnectionBuilder.ToString()))
            {
                connection.Open();

                try
                {
                    using (var command = new SqlCommand($"SELECT * FROM [{sourceSchema}].[{sourceTable}]", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (var dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int rowCount = 0;

                            while (dataReader.Read())
                            {
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

                                try
                                {
                                    client.Document.Store(fullTargetSchema, new KbDocument(dbObject));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }

                                rowCount++;
                            }
                        }
                    }
                    connection.Close();
                }
                catch
                {
                    //TODO: add error handling/logging
                    throw;
                }

                client.Transaction.Commit();
            }
        }

    }
}