using Newtonsoft.Json;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using System;

namespace DatabaseObjectViewer
{
    public partial class FormMain : Form
    {
        private readonly List<Type> _types = new()
        {
            typeof(PhysicalDocument),
            typeof(PhysicalDocumentPage),
            typeof(PhysicalDocumentPageCatalog),
            typeof(PhysicalDocumentPageCatalogItem),
            typeof(PhysicalDocumentPageMap),
            typeof(PhysicalIndex),
            typeof(PhysicalIndexCatalog),
            typeof(PhysicalIndexEntry),
            typeof(PhysicalIndexLeaf),
            typeof(PhysicalIndexPages)
        };

        public FormMain()
        {
            InitializeComponent();

            textBoxFile.AllowDrop = true;
            textBoxFile.DragEnter += TextBoxFile_DragEnter;
            textBoxFile.DragDrop += TextBoxFile_DragDrop;

            AllowDrop = true;
            DragEnter += TextBoxFile_DragEnter;
            DragDrop += TextBoxFile_DragDrop;

            comboBoxType.Items.Add("<unknown>");
            _types.Select(o => o.Name).Order().ToList().ForEach(t => comboBoxType.Items.Add(t));

            Resize += FormMain_Resize;

            FormMain_Resize(this, new EventArgs());
        }

        private void FormMain_Resize(object? sender, EventArgs e)
        {
            textBoxObject.Width = (ClientSize.Width - textBoxObject.Left) - 20;
            textBoxObject.Height = (ClientSize.Height - textBoxObject.Top) - 20;
        }

        private void TextBoxFile_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TextBoxFile_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0)
                {
                    textBoxFile.Text = files[0];
                    ProcessFile(textBoxFile.Text);
                }
            }
        }

        private void ProcessFile(string fileName)
        {
            foreach (var type in _types)
            {
                if (ProcessFilePBuf(fileName, type, out var friendlyText))
                {
                    textBoxObject.Text = friendlyText;
                    comboBoxType.Text = type.Name;
                    return;
                }
                else if (ProcessFileJson(fileName, type, out friendlyText))
                {
                    textBoxObject.Text = friendlyText;
                    comboBoxType.Text = type.Name;
                    return;
                }
            }

            textBoxObject.Text = $"Could not desearilize the object: '{fileName}'.";
            comboBoxType.Text = "<unknown>";
        }

        private bool ProcessFilePBuf(string fileName, Type type, out string friendlyText)
        {
            try
            {
                var fileBytes = File.ReadAllBytes(fileName);

                try
                {
                    var serializedData = NTDLS.Katzebase.Engine.Library.Compression.Deflate.Decompress(fileBytes);
                    fileBytes = serializedData;
                }
                catch {}

                using var input = new MemoryStream(fileBytes);

                var deserializeMethod = typeof(ProtoBuf.Serializer)?
                    .GetMethod("Deserialize", new Type[] { typeof(Stream) })?
                    .MakeGenericMethod(type);

                var deserializedObject = deserializeMethod?.Invoke(null, new object[] { input });

                friendlyText = JsonConvert.SerializeObject(deserializedObject, Newtonsoft.Json.Formatting.Indented);
                return true;
            }
            catch
            {
                friendlyText = "";
                return false;
            }
        }

        private bool ProcessFileJson(string fileName, Type type, out string friendlyText)
        {
            try
            {
                var fileBytes = File.ReadAllBytes(fileName);
                var serializedData = NTDLS.Katzebase.Engine.Library.Compression.Deflate.DecompressToString(fileBytes);

                var deserializeMethod = typeof(JsonConvert)?
                        .GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) });

                // Invoke the DeserializeObject method to deserialize the JSON string
                var deserializedObject = deserializeMethod?.Invoke(null, new object[] { serializedData, type });

                friendlyText = Newtonsoft.Json.JsonConvert.SerializeObject(deserializedObject, Newtonsoft.Json.Formatting.Indented);
                return true;
            }
            catch
            {
                friendlyText = "";
                return false;
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select a File";

                openFileDialog.Filter = "All Files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxFile.Text = openFileDialog.FileName;
                    ProcessFile(textBoxFile.Text);
                }
            }
        }
    }
}
