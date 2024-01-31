using NTDLS.Katzebase.Client;

namespace NTDLS.Katzebase.UI
{
    public partial class FormConnect : Form
    {
        public string ServerHost => textBoxServerAddress.Text.Trim();
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
        }

        private void FormConnect_Load(object sender, EventArgs e)
        {
            textBoxServerAddress.Text = "127.0.0.1";
            textBoxPort.Text = "6858";

            AcceptButton = buttonConnect;
            CancelButton = buttonCancel;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            try
            {
                using (var client = new KbClient(ServerHost, ServerPort))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to connect to the specified server: \"{ex.Message}\".", KbConstants.FriendlyName);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}