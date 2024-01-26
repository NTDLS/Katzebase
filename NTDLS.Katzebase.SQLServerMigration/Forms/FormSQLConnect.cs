using System.ComponentModel;
using System.Data.SqlClient;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public partial class FormSQLConnect : Form
    {
        #region Local Types.

        class ServerListObject
        {
            public string? ServerName { get; set; }
            public string? Database { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public bool IntegratedSecurity { get; set; }
            public bool SSLConnection { get; set; }
        }

        public class CheckConnectivity_Result
        {
            public bool Success { get; set; }
            public bool IsAdmin { get; set; }
            public string? ExceptionMessage { get; set; }

        }

        #endregion

        public SQLConnectionDetails ConnectionDetails { get; set; } = new();
        public CheckConnectivity_Result ConnectivityResult { get; set; } = new();


        #region Constructors / Destructors.

        public FormSQLConnect()
        {
            InitializeComponent();

            comboBoxDatabaseName.DropDown += CboDatabaseName_DropDown;
        }

        #endregion

        #region Public Mehtods.

        public string Caption
        {
            get { return this.Text.Trim(); }
            set { this.Text = value.Trim(); }
        }

        #endregion

        private void SQLConnectForm_Load(object sender, EventArgs e)
        {
            AcceptButton = cmdOk;
            CancelButton = cmdCancel;

#if DEBUG
            comboBoxDatabaseName.Text = "AdventureWorks";
#endif

        }

        private CheckConnectivity_Result CheckConnectivity()
        {
            return CheckConnectivity(string.Empty);
        }

        #region CheckConnectivity().

        AbortableBackgroundWorker? checkConnectivity_Worker = null;
        FormProgress? checkConnectivityProgress = null;

        private CheckConnectivity_Result CheckConnectivity(string alternateDatabase)
        {
            var result = new CheckConnectivity_Result()
            {
                Success = false
            };
            using (checkConnectivityProgress = new FormProgress())
            {
                checkConnectivityProgress.OnCancel += CheckConnectivityProgress_OnCancel;
                checkConnectivityProgress.SetCanCancel(true);
                checkConnectivityProgress.SetBodyText("Checking connectivity...");

                checkConnectivity_Worker = new AbortableBackgroundWorker();
                checkConnectivity_Worker.RunWorkerCompleted += CheckConnectivity_Worker_RunWorkerCompleted;
                checkConnectivity_Worker.DoWork += CheckConnectivity_Worker_DoWork;
                checkConnectivity_Worker.WorkerReportsProgress = false;
                checkConnectivity_Worker.WorkerSupportsCancellation = true;
                checkConnectivity_Worker.RunWorkerAsync("master");

                checkConnectivityProgress.ShowDialog();

                var workerResult = checkConnectivityProgress.UserData as CheckConnectivity_Result;

                if (workerResult != null)
                {
                    result = workerResult;
                    //This is so we dont show a message box on cancellation.
                    if (string.IsNullOrEmpty(result.ExceptionMessage) == false && result.ExceptionMessage.ToLower().Contains(/*the */"thread was"/* being terminated*/))
                    {
                        result.ExceptionMessage = string.Empty;
                    }
                }
            }

            return result;
        }

        private void CheckConnectivityProgress_OnCancel(object sender, FormProgress.OnCancelInfo e)
        {
            if (checkConnectivityProgress != null)
                checkConnectivityProgress.SetBodyText("Cancelling...");

            if (checkConnectivity_Worker != null)
                checkConnectivity_Worker.CancelAsync();
        }

        private void CheckConnectivity_Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs? e)
        {
            if (checkConnectivityProgress != null && e != null)
            {
                checkConnectivityProgress.UserData = e.Result;
                checkConnectivityProgress.Close();
            }
        }

        private void CheckConnectivity_Worker_DoWork(object? sender, DoWorkEventArgs? e)
        {
            if (e == null)
            {
                return;
            }

            var _child = new Thread(() =>
            {
                var alternateDatabase = e.Argument?.ToString();

                e.Result = new CheckConnectivity_Result()
                {
                    Success = false
                };

                var builder = ConnectionDetails.ConnectionBuilder;

                if (alternateDatabase != null && alternateDatabase != string.Empty)
                {
                    builder.InitialCatalog = alternateDatabase;
                }

                using (var sqlConnection = new SqlConnection(builder.ToString()))
                {
                    try
                    {
                        sqlConnection.Open();

                        var sqlCommand = new SqlCommand("SELECT IS_SRVROLEMEMBER('sysadmin')", sqlConnection);
                        SqlDataReader? sqlReader = null;

                        try
                        {
                            sqlReader = sqlCommand.ExecuteReader();
                            if (sqlReader.Read())
                            {
                                ((CheckConnectivity_Result)e.Result).IsAdmin = ParseBool(sqlReader[0]);
                            }
                            else
                            {
                                throw new Exception("SQL server role could not be determined.");
                            }
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            if (sqlReader != null)
                            {
                                sqlReader.Close();
                            }
                        }

                        ((CheckConnectivity_Result)e.Result).Success = true;
                    }
                    catch (Exception ex)
                    {
                        ((CheckConnectivity_Result)e.Result).Success = false;
                        ((CheckConnectivity_Result)e.Result).ExceptionMessage = ex.Message;
                    }
                    finally
                    {
                        if (sqlConnection != null)
                        {
                            sqlConnection.Close();
                        }
                    }
                }

            });

            _child.Start();
            while (_child?.IsAlive == true)
            {
                if ((sender as BackgroundWorker)?.CancellationPending == true)
                {
                    _child?.Interrupt();
                    _child?.Join();
                }
                Thread.Sleep(1);
            }
        }

        #endregion

        private void PushConnectionInfo()
        {
            ConnectionDetails.UserName = txtUsername.Text;
            ConnectionDetails.Password = txtPassword.Text;
            ConnectionDetails.ServerName = textBoxServer.Text;
            ConnectionDetails.DatabaseName = comboBoxDatabaseName.Text;
            ConnectionDetails.UseIntegratedSecurity = checkBoxIntegratedSecurity.Checked;
            ConnectionDetails.EncryptConnection = checkBoxSSLConnection.Checked;

        }

        private void cmdOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(comboBoxDatabaseName.Text))
            {
                MessageBox.Show("Please select a database.", "Migration Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PushConnectionInfo();

            ConnectivityResult = CheckConnectivity();

            if (ConnectivityResult.Success)
            {
                if (ConnectivityResult.IsAdmin == false)
                {
                    if (MessageBox.Show(
                        "The use you are connecting is not an administrator. It cannot be guaranteed that you will have access to script all selected objects.\r\n\r\nContinue Anyway?",
                        "User Permission Check", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            ConnectivityResult = new CheckConnectivity_Result();
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void CboDatabaseName_DropDown(object? sender, EventArgs e)
        {
            PushConnectionInfo();

            var result = CheckConnectivity("master");

            if (result.Success == false)
            {
                if (string.IsNullOrEmpty(result.ExceptionMessage) == false)
                {
                    MessageBox.Show(result.ExceptionMessage, "Connection failed.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                return;
            }

            try
            {
                string currentText = comboBoxDatabaseName.Text;

                comboBoxDatabaseName.Items.Clear();

                var builder = ConnectionDetails.ConnectionBuilder;
                builder.InitialCatalog = "master";

                using (var connection = new SqlConnection(builder.ToString()))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name ASC", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                comboBoxDatabaseName.Items.Add(reader[0]?.ToString() ?? string.Empty);
                            }
                        }
                    }

                    connection.Close();
                }

                comboBoxDatabaseName.Text = currentText;
            }
            catch { }
        }

        private void cbIntegratedSecurity_CheckedChanged(object sender, EventArgs e)
        {
            txtUsername.Enabled = !checkBoxIntegratedSecurity.Checked;
            txtPassword.Enabled = !checkBoxIntegratedSecurity.Checked;
        }

        public static bool ParseBool(object? value)
        {
            value = value?.ToString() ?? string.Empty;
            string sValue = ((string)value).Replace(",", "").ToLower();

            if (sValue == "true" || sValue == "yes" || sValue == "on")
            {
                return true;
            }
            else if (sValue == "false" || sValue == "no" || sValue == "off")
            {
                return false;
            }
            else
            {
                if (int.TryParse(sValue, out int intValue))
                {
                    return intValue != 0;
                }
                else
                {
                    throw new Exception("Could not parse boolean value.");
                }
            }
        }
    }
}
