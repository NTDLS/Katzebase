using ICSharpCode.AvalonEdit;
using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using System.Text;

namespace Katzebase.UI.Classes
{
    internal class TabFilePage : TabPage
    {
        public int ExecutionExceptionCount { get; private set; } = 0;
        public bool IsScriptExecuting { get; private set; } = false;

        private bool _isSaved = false;

        public bool IsSaved
        {
            get => _isSaved;

            set
            {
                _isSaved = value;
                if (_isSaved == true)
                {
                    this.Text = Text.TrimEnd('*');
                }
            }
        }

        public SplitContainer ThisSplitContainer = new()
        {
            Orientation = Orientation.Horizontal,
            Dock = DockStyle.Fill,
        };

        public bool CollapseSplitter
        {
            get => ThisSplitContainer.Panel2Collapsed;
            set
            {
                ThisSplitContainer.Panel2Collapsed = value;
            }
        }

        public TabPage OutputTab { get; private set; } = new("Output");
        public TabPage ResultsTab { get; private set; } = new("Results");
        public RichTextBox OutputTextbox { get; private set; } = new() { Dock = DockStyle.Fill };
        public DataGridView OutputGrid { get; private set; } = new() { Dock = DockStyle.Fill };
        public TabControl BottomTabControl { get; private set; } = new() { Dock = DockStyle.Fill };

        public TextEditor Editor { get; private set; }
        public FormFindText FindTextForm { get; private set; }
        public FormReplaceText ReplaceTextForm { get; private set; }
        public string ServerAddressURL { get; set; }
        public KatzebaseClient? Client { get; private set; }
        public bool IsFileOpen { get; private set; } = false;

        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set
            {
                this.Text = Path.GetFileName(value);
                _filePath = value;
            }
        }

        public TabFilePage(string serverAddressURL, string tabText, TextEditor editor) :
             base(tabText)
        {
            Editor = editor;
            FindTextForm = new FormFindText(this);
            ReplaceTextForm = new FormReplaceText(this);
            ServerAddressURL = serverAddressURL;
            if (string.IsNullOrEmpty(serverAddressURL) == false)
            {
                Client = new KatzebaseClient(ServerAddressURL);
            }
        }

        public void OpenFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                Editor.Document.FileName = filePath;
                Editor.Text = File.ReadAllText(Editor.Document.FileName);
                IsSaved = true;
            }

            this.FilePath = filePath;

            IsFileOpen = true;
        }

        public bool Save(string fileName)
        {
            File.WriteAllText(fileName, Editor.Text);
            IsSaved = true;
            this.OpenFile(fileName);
            return true;
        }

        public bool Save()
        {
            if (IsFileOpen)
            {
                File.WriteAllText(FilePath, Editor.Text);
                IsSaved = true;
                return true;
            }
            return false;
        }

        public static TabFilePage Create(EditorFactory editorFactory, string tabText = "", string serverAddress = "")
        {
            if (string.IsNullOrWhiteSpace(tabText))
            {
                tabText = FormUtility.GetNextNewFileName();
            }

            var tabFilePage = editorFactory.Create(serverAddress, tabText);

            //tabFilePage.Editor.KeyUp += Editor_KeyUp;
            //tabFilePage.Editor.Drop += Editor_Drop;

            tabFilePage.Controls.Add(tabFilePage.ThisSplitContainer);

            tabFilePage.ThisSplitContainer.Panel1.Controls.Add(new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = tabFilePage.Editor
            });

            tabFilePage.ThisSplitContainer.Panel2.Controls.Add(tabFilePage.BottomTabControl);

            tabFilePage.BottomTabControl.TabPages.Add(tabFilePage.OutputTab); //Add output tab to bottom.
            tabFilePage.OutputTab.Controls.Add(tabFilePage.OutputTextbox);

            tabFilePage.BottomTabControl.TabPages.Add(tabFilePage.ResultsTab); //Add results tab to bottom.
            tabFilePage.ResultsTab.Controls.Add(tabFilePage.OutputGrid);

            tabFilePage.Client?.Server.Ping();

            tabFilePage.Editor.Focus();

            return tabFilePage;
        }

        #region Execute Current Script.

        /// <summary>
        /// This is for actually executing the script against a live database.
        /// </summary>
        public void ExecuteCurrentScriptAsync(bool justExplain)
        {
            try
            {
                if (IsScriptExecuting)
                {
                    return;
                }
                IsScriptExecuting = true;

                if (Client == null)
                {
                    IsScriptExecuting = false;
                    return;
                }

                PreExecuteEvent(this);

                OutputGrid.Rows.Clear();
                OutputGrid.Columns.Clear();

                string scriptText = Editor.Text;

                if (Editor.SelectionLength > 0)
                {
                    scriptText = Editor.SelectedText;
                }

                Task.Run(() =>
                {
                    ExecuteCurrentScriptSync(Client, scriptText, justExplain);
                }).ContinueWith((t) =>
                {
                    PostExecuteEvent(this);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", PublicLibrary.Constants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PreExecuteEvent(TabFilePage tabFilePage)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<TabFilePage>(PreExecuteEvent), this);
                    return;
                }

                OutputTextbox.Text = "";
                ExecutionExceptionCount = 0;

                ThisSplitContainer.Panel2Collapsed = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", PublicLibrary.Constants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PostExecuteEvent(TabFilePage tabFilePage)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<TabFilePage>(PostExecuteEvent), tabFilePage);
                    return;
                }

                ThisSplitContainer.Panel2Collapsed = false;

                if (OutputGrid.RowCount > 0)
                {
                    BottomTabControl.SelectedTab = ResultsTab;
                }
                else
                {
                    BottomTabControl.SelectedTab = OutputTab;
                }

                IsScriptExecuting = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", PublicLibrary.Constants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExecuteCurrentScriptSync(KatzebaseClient client, string scriptText, bool justExplain)
        {
            WorkloadGroup group = new WorkloadGroup();

            try
            {
                group.OnException += Group_OnException;
                group.OnStatus += Group_OnStatus;

                //TODO: This needs to be MUCH more intelligent! What about ';' in strings? ... :/
                var scripts = scriptText.Split(";").Where(o => string.IsNullOrWhiteSpace(o) == false).ToList();

                int batchNumber = 1;

                foreach (var script in scripts)
                {
                    DateTime startTime = DateTime.UtcNow;

                    KbQueryResult result;

                    if (justExplain)
                    {
                        result = client.Query.ExplainQuery(script);
                    }
                    else
                    {
                        result = client.Query.ExecuteQuery(script);
                    }

                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    AppendToOutput($"Batch {batchNumber:n0} of {scripts.Count} completed in {duration:N0}ms.", Color.Black);

                    if (justExplain && string.IsNullOrWhiteSpace(result.Explanation) == false)
                    {
                        AppendToOutput(result.Explanation, Color.DarkGreen);
                    }

                    if (result.Metrics?.Count > 0)
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine("Metrics {");

                        foreach (var wt in result.Metrics.Where(o => o.Value >= 0.5).OrderBy(o => o.Value))
                        {
                            stringBuilder.Append($"\t{wt.MetricType} {wt.Name}: {wt.Value:n0}");
                            if (wt.MetricType == PublicLibrary.Constants.KbMetricType.Cumulative)
                            {
                                stringBuilder.Append($" (count: {wt.Count:n0})");
                            }

                            stringBuilder.AppendLine();
                        }

                        stringBuilder.AppendLine($"}}");

                        AppendToOutput(stringBuilder.ToString(), Color.DarkBlue);

                    }

                    PopulateResultsGrid(result);

                    if (string.IsNullOrWhiteSpace(result.Message) == false)
                    {
                        AppendToOutput($"{result.Message}", Color.Black);
                    }

                    batchNumber++;
                }
            }
            catch (KbExceptionBase ex)
            {
                Group_OnException(group, ex);
            }
            catch (Exception ex)
            {
                Group_OnException(group, new KbExceptionBase(ex.Message));
            }
        }

        private void PopulateResultsGrid(KbQueryResult result)
        {
            try
            {
                if (result.Rows.Count == 0)
                {
                    return;
                }

                if (InvokeRequired)
                {
                    Invoke(new Action<KbQueryResult>(PopulateResultsGrid), result);
                    return;
                }

                OutputGrid.SuspendLayout();

                foreach (var field in result.Fields)
                {
                    OutputGrid.Columns.Add(field.Name, field.Name);
                }

                int maxRowsToLoad = 100;
                foreach (var row in result.Rows)
                {
                    var rowValues = new List<string>();

                    for (int fieldIndex = 0; fieldIndex < result.Fields.Count; fieldIndex++)
                    {
                        var fieldValue = row.Values[fieldIndex];
                        rowValues.Add(fieldValue ?? string.Empty);
                    }

                    OutputGrid.Rows.Add(rowValues.ToArray());

                    maxRowsToLoad--;
                    if (maxRowsToLoad <= 0)
                    {
                        break;
                    }
                }

                OutputGrid.ResumeLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", PublicLibrary.Constants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion


        private void Group_OnStatus(WorkloadGroup sender, string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<WorkloadGroup, string, Color>(Group_OnStatus), sender, text, color);
                return;
            }

            AppendToOutput(text, color);
        }

        private void AppendToOutput(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, Color>(AppendToOutput), text, color);
                return;
            }

            OutputTextbox.SelectionStart = OutputTextbox.TextLength;
            OutputTextbox.SelectionLength = 0;

            OutputTextbox.SelectionColor = color;
            OutputTextbox.AppendText($"{text}\r\n");
            OutputTextbox.SelectionColor = OutputTextbox.ForeColor;

            OutputTextbox.SelectionStart = OutputTextbox.Text.Length;
            OutputTextbox.ScrollToCaret();
        }

        private void Group_OnException(WorkloadGroup sender, KbExceptionBase ex)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<WorkloadGroup, KbExceptionBase>(Group_OnException), sender, ex);
                return;
            }

            ExecutionExceptionCount++;

            ThisSplitContainer.Panel2Collapsed = false;

            AppendToOutput($"Exception: {ex.Message}\r\n", Color.DarkRed);
        }
    }
}
