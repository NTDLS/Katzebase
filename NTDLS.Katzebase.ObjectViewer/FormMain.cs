using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.PersistentTypes.Procedure;
using NTDLS.Katzebase.PersistentTypes.Schema;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.ObjectViewer
{
    public partial class FormMain : Form
    {
        class TypeMapping(string identifier, Type type, Shared.EngineConstants.IOFormat format)
        {
            public string Identifier { get; set; } = identifier.ToLowerInvariant();
            public IOFormat Format { get; set; } = format;
            public Type Type { get; set; } = type;
        }

        private readonly List<TypeMapping> _types = new()
        {
            new TypeMapping(".kbixpage", typeof(PhysicalIndexPages), IOFormat.PBuf),
            new TypeMapping(".kbpage", typeof(PhysicalDocumentPage), IOFormat.PBuf),
            new TypeMapping(".kbmap", typeof(PhysicalDocumentPageMap), IOFormat.PBuf),
            new TypeMapping("@schemas.kbcat", typeof(PhysicalSchemaCatalog), IOFormat.JSON),
            new TypeMapping("@pages.kbcat", typeof(PhysicalDocumentPageCatalog), IOFormat.PBuf),
            new TypeMapping("@indexes.kbcat", typeof(PhysicalIndexCatalog), IOFormat.JSON),
            new TypeMapping("@procedures.kbcat", typeof(PhysicalProcedureCatalog), IOFormat.JSON),
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

        private void ProcessFile(string filePath)
        {
            try
            {
                var typeMapping = _types.FirstOrDefault(
                    o => o.Identifier.Is(Path.GetFileName(filePath))
                    || o.Identifier.Is(Path.GetExtension(filePath)));

                if (typeMapping != null)
                {
                    if (typeMapping.Format == IOFormat.PBuf)
                    {
                        if (ProcessFilePBuf(filePath, typeMapping.Type, out var friendlyText))
                        {
                            textBoxObject.Text = friendlyText;
                            textBoxType.Text = typeMapping.Type.Name;
                            return;
                        }
                    }
                    else if (typeMapping.Format == IOFormat.JSON)
                    {
                        if (ProcessFileJson(filePath, typeMapping.Type, out var friendlyText))
                        {
                            textBoxObject.Text = friendlyText;
                            textBoxType.Text = typeMapping.Type.Name;
                            return;
                        }
                    }
                }
            }
            catch
            {
            }

            textBoxObject.Text = $"Could not deserialize the object: '{filePath}'.";
            textBoxType.Text = "<unknown>";
        }

        private static bool ProcessFilePBuf(string fileName, Type type, out string friendlyText)
        {
            try
            {
                var fileBytes = File.ReadAllBytes(fileName);

                try
                {
                    var serializedData = Shared.Compression.Deflate.Decompress(fileBytes);
                    fileBytes = serializedData;
                }
                catch { }

                using var input = new MemoryStream(fileBytes);

                var deserializeMethod = typeof(ProtoBuf.Serializer)?
                    .GetMethod("Deserialize", [typeof(Stream)])?
                    .MakeGenericMethod(type);

                var deserializedObject = deserializeMethod?.Invoke(null, [input]);

                friendlyText = JsonConvert.SerializeObject(deserializedObject, Formatting.Indented);
                return true;
            }
            catch
            {
                friendlyText = "";
                return false;
            }
        }

        private static bool ProcessFileJson(string fileName, Type type, out string friendlyText)
        {
            try
            {
                var plainText = File.ReadAllText(fileName);

                var deserializeMethod = typeof(JsonConvert)?
                        .GetMethod("DeserializeObject", [typeof(string), typeof(Type)]);

                // Invoke the DeserializeObject method to deserialize the JSON string
                var deserializedObject = deserializeMethod?.Invoke(null, [plainText, type]);

                friendlyText = JsonConvert.SerializeObject(deserializedObject, Formatting.Indented);
                return true;
            }
            catch
            {
                friendlyText = "";
                return false;
            }
        }

        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog();
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
