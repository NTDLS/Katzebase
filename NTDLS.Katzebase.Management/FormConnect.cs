using NTDLS.Katzebase.Client;

namespace NTDLS.Katzebase.Management
{
    public partial class FormConnect : Form
    {
        public string ServerHost => textBoxServerAddress.Text.Trim();
        public string Username => textBoxUsername.Text.Trim();
        public string PasswordHash => KbClient.HashPassword(textBoxPassword.Text.Trim());

        public int ServerPort
        {
            get
            {
                _ = int.TryParse(textBoxPort.Text.Trim(), out var port);
                return port;
            }
        }

        public FormConnect()
        {
            InitializeComponent();

            textBoxServerAddress.Text = "127.0.0.1";
            textBoxPort.Text = "6858";
            textBoxUsername.Text = "admin";
        }

        public FormConnect(string host, int port, string username)
        {
            InitializeComponent();

            textBoxServerAddress.Text = host;
            textBoxPort.Text = port.ToString();
            textBoxUsername.Text = username;
        }

        private void FormConnect_Load(object sender, EventArgs e)
        {
            AcceptButton = buttonConnect;
            CancelButton = buttonCancel;

            Shown += FormConnect_Shown;
        }

        private void FormConnect_Shown(object? sender, EventArgs e)
        {
            textBoxPassword.Focus();

        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            try
            {
                using var client = new KbClient(ServerHost, ServerPort, Username, PasswordHash, $"{KbConstants.FriendlyName}.UI.Query");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to connect to the specified server: \"{ex.Message}\".", KbConstants.FriendlyName);
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}