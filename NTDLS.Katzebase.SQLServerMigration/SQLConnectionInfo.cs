using System.Data.SqlClient;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public class SQLConnectionDetails
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public bool UseIntegratedSecurity { get; set; } = true;
        public string DatabaseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool EncryptConnection { get; set; } = false;
        public string ApplicationName { get; set; } = string.Empty;

        public SQLConnectionDetails()
        {
        }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(UserName)
                    && string.IsNullOrEmpty(Password)
                    && string.IsNullOrEmpty(ServerName)
                    && string.IsNullOrEmpty(DatabaseName)
                    && string.IsNullOrEmpty(Description)
                    && string.IsNullOrEmpty(ApplicationName);
            }
        }

        public SQLConnectionDetails(SqlConnectionStringBuilder connectionStringBuilder)
        {
            ServerName = connectionStringBuilder.DataSource;
            UserName = connectionStringBuilder.UserID;
            Password = connectionStringBuilder.Password;
            DatabaseName = connectionStringBuilder.InitialCatalog;
            UseIntegratedSecurity = connectionStringBuilder.IntegratedSecurity;
        }

        public SQLConnectionDetails(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

            ServerName = connectionStringBuilder.DataSource;
            UserName = connectionStringBuilder.UserID;
            Password = connectionStringBuilder.Password;
            DatabaseName = connectionStringBuilder.InitialCatalog;
            UseIntegratedSecurity = connectionStringBuilder.IntegratedSecurity;
        }

        private string? _friendlyName = null;
        public string FriendlyName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_friendlyName))
                {
                    return ServerName + ((DatabaseName ?? string.Empty) == string.Empty ? "" : ":" + DatabaseName);
                }
                return _friendlyName;
            }
            set
            {
                _friendlyName = value;
            }
        }

        public new string ToString()
        {
            return ConnectionBuilder.ToString();
        }

        public SqlConnectionStringBuilder ConnectionStringAlternateDatabase(string databaseName)
        {
            if (databaseName != null && databaseName != string.Empty)
            {
                var connectionString = ConnectionBuilder;
                connectionString.InitialCatalog = databaseName;
                return connectionString;
            }
            return ConnectionBuilder;
        }

        public SqlConnectionStringBuilder ConnectionBuilder
        {
            get
            {
                var builder = new SqlConnectionStringBuilder();

                builder.DataSource = ServerName;
                if (ApplicationName != string.Empty && ApplicationName != null)
                {
                    builder.ApplicationName = ApplicationName;
                }
                builder.InitialCatalog = DatabaseName;

                if (UseIntegratedSecurity)
                {
                    builder.IntegratedSecurity = true;
                }
                else
                {
                    builder.IntegratedSecurity = false;
                    builder.UserID = UserName;
                    builder.Password = Password;
                }

                if (EncryptConnection)
                {
                    builder.Encrypt = true;
                    builder.TrustServerCertificate = true;
                }
                else
                {
                    builder.Encrypt = false;
                    builder.TrustServerCertificate = false;
                }

                return builder;
            }
        }
    }
}
