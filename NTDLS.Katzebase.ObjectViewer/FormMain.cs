using Newtonsoft.Json;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Functions.Procedures.Persistent;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Schemas;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.ObjectViewer
{
    public partial class FormMain : Form
    {
        class TypeMapping
        {
            public string Identifier { get; set; }
            public IOFormat Format { get; set; }
            public Type Type { get; set; }

            public TypeMapping(string identifier, Type type, IOFormat format)
            {
                Identifier = identifier.ToLower();
                Type = type;
                Format = format;
            }
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
                var typeMapping = _types.Where(o => o.Identifier == Path.GetFileName(filePath).ToLower()
                || o.Identifier == Path.GetExtension(filePath).ToLower()).ToList().FirstOrDefault();
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

            textBoxObject.Text = $"Could not desearilize the object: '{filePath}'.";
            textBoxType.Text = "<unknown>";
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
                catch { }

                using var input = new MemoryStream(fileBytes);

                var deserializeMethod = typeof(ProtoBuf.Serializer)?
                    .GetMethod("Deserialize", new Type[] { typeof(Stream) })?
                    .MakeGenericMethod(type);

                var deserializedObject = deserializeMethod?.Invoke(null, new object[] { input });

                friendlyText = JsonConvert.SerializeObject(deserializedObject, Formatting.Indented);
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

                friendlyText = JsonConvert.SerializeObject(deserializedObject, Formatting.Indented);
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
