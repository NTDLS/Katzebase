using Katzebase.PublicLibrary.Client;

namespace Katzebase.UI
{
    public partial class FormConnect : Form
    {
        public string ServerAddress => textBoxServerAddress.Text.Trim();
        public string ServerPort => textBoxPort.Text.Trim();
        public string ServerAddressURL => $"http://{ServerAddress}:{ServerPort}/";

        public FormConnect()
        {
            InitializeComponent();
        }

        private void FormConnect_Load(object sender, EventArgs e)
        {
            textBoxServerAddress.Text = "localhost";
            textBoxPort.Text = "6858";

            AcceptButton = buttonConnect;
            AcceptButton = buttonCancel;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            try
            {
                var client = new KatzebaseClient(ServerAddressURL);
                if (client.Server.Ping())
                {
                    DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to connect to the specified server: \"{ex.Message}\".", PublicLibrary.Constants.FriendlyName);
            }

        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}