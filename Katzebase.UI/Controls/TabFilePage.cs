using ICSharpCode.AvalonEdit;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Katzebase.UI.Classes;
using System.Runtime.CompilerServices;
using System.Text;

namespace Katzebase.UI.Controls
{
    internal class TabFilePage : TabPage
    {
        #region Properties.

        public int ExecutionExceptionCount { get; private set; } = 0;
        public bool IsScriptExecuting { get; private set; } = false;
        public string ServerAddressURL { get; set; }
        public KatzebaseClient? Client { get; private set; }
        public bool IsFileOpen { get; private set; } = false;


        private bool _isSaved = false;
        public bool IsSaved
        {
            get => _isSaved;

            set
            {
                _isSaved = value;
                if (_isSaved == true)
                {
                    Text = Text.TrimEnd('*');
                }
            }
        }

        public SplitContainer TabSplitContainer = new()
        {
            Orientation = Orientation.Horizontal,
            Dock = DockStyle.Fill,
            Panel2Collapsed = true
        };

        public bool CollapseSplitter
        {
            get => TabSplitContainer.Panel2Collapsed;
            set
            {
                TabSplitContainer.Panel2Collapsed = value;
            }
        }

        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set
            {
                Text = Path.GetFileName(value);
                _filePath = value;
            }
        }

        #endregion

        #region Controls.

        public TabPage OutputTab { get; private set; } = new("Output");
        public TabPage ResultsTab { get; private set; } = new("Results");
        public RichTextBox OutputTextbox { get; private set; } = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font("Courier New", 10, FontStyle.Regular),
            WordWrap = false,
        };
        public DataGridView OutputGrid { get; private set; } = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowDrop = false,
            AllowUserToDeleteRows = false,
            ShowEditingIcon = false,
            ShowCellErrors = false,
            ShowCellToolTips = false,
            ReadOnly = true,
            AllowUserToOrderColumns = true,
            AllowUserToResizeRows = true,
            AllowUserToResizeColumns = true,
        };

        public TabControl BottomTabControl { get; private set; } = new() { Dock = DockStyle.Fill };
        public TextEditor Editor { get; private set; }
        public FormFindText FindTextForm { get; private set; }
        public FormReplaceText ReplaceTextForm { get; private set; }

        #endregion

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

        public static TabFilePage Create(EditorFactory editorFactory, string tabText = "", string serverAddress = "")
        {
            if (string.IsNullOrWhiteSpace(tabText))
            {
                tabText = FormUtility.GetNextNewFileName();
            }

            var tabFilePage = editorFactory.Create(serverAddress, tabText);

            tabFilePage.Editor.KeyUp += tabFilePage.Editor_KeyUp;

            tabFilePage.Controls.Add(tabFilePage.TabSplitContainer);

            tabFilePage.TabSplitContainer.Panel1.Controls.Add(new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = tabFilePage.Editor
            });

            tabFilePage.TabSplitContainer.Panel2.Controls.Add(tabFilePage.BottomTabControl);

            tabFilePage.BottomTabControl.TabPages.Add(tabFilePage.OutputTab); //Add output tab to bottom.
            tabFilePage.OutputTab.Controls.Add(tabFilePage.OutputTextbox);

            tabFilePage.BottomTabControl.TabPages.Add(tabFilePage.ResultsTab); //Add results tab to bottom.
            tabFilePage.ResultsTab.Controls.Add(tabFilePage.OutputGrid);

            tabFilePage.TabSplitContainer.SplitterMoved += TabSplitContainer_SplitterMoved;

            tabFilePage.TabSplitContainer.SplitterDistance = Preferences.Instance.ResultsSplitterDistance;

            tabFilePage.Client?.Server.Ping();

            tabFilePage.Editor.Focus();

            return tabFilePage;
        }

        private static void TabSplitContainer_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            if (sender is SplitContainer)
            {
                Preferences.Instance.ResultsSplitterDistance = ((SplitContainer)(sender)).SplitterDistance;
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

            FilePath = filePath;

            IsFileOpen = true;
        }

        public bool Save(string fileName)
        {
            File.WriteAllText(fileName, Editor.Text);
            IsSaved = true;
            OpenFile(fileName);
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

        private void Editor_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F5)
            {
                ExecuteCurrentScriptAsync(false);
            }
        }

        #region Execute.

        public void ExecuteStopCommand()
        {
            if (IsScriptExecuting == false)
            {
                return;
            }
            if (Client == null)
            {
                IsScriptExecuting = false;
                return;
            }


            Client.Server.TerminateProcess(Client.ServerProcessId);
        }

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

                Client.Server.Ping();

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
                MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                TabSplitContainer.Panel2Collapsed = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                TabSplitContainer.Panel2Collapsed = false;

                if (OutputGrid.RowCount > 0)
                {
                    BottomTabControl.SelectedTab = ResultsTab;
                }
                else
                {
                    BottomTabControl.SelectedTab = OutputTab;
                }

                OutputGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                tabFilePage.Editor.Focus();

                IsScriptExecuting = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExecuteCurrentScriptSync(KatzebaseClient client, string scriptText, bool justExplain)
        {
            WorkloadGroup group = new WorkloadGroup();

            try
            {
                group.OnException += Group_OnException;
                group.OnStatus += Group_OnStatus;

                var scripts = KbUtility.SplitQueryBatchesOnGO(scriptText);

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
                    AppendToOutput($"Batch {batchNumber:n0} of {scripts.Count} completed in {duration:N0}ms.  ({result.RowCount} rows affected)", Color.Black);

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
                            stringBuilder.Append($"  {wt.Name} -> Total: {wt.Value:n0}");
                            if (wt.MetricType == KbConstants.KbMetricType.Cumulative)
                            {
                                stringBuilder.Append($", Count: {wt.Count:n0}, Average: {wt.Value / wt.Count:n2}");
                            }
                            stringBuilder.AppendLine();
                        }

                        stringBuilder.AppendLine($"}}");

                        AppendToOutput(stringBuilder.ToString(), Color.DarkBlue);

                    }

                    PopulateResultsGrid(result);

                    foreach (var message in result.Messages)
                    {
                        if (message.MessageType == KbConstants.KbMessageType.Verbose)
                            AppendToOutput($"{message.Text}", Color.Black);
                        else if (message.MessageType == KbConstants.KbMessageType.Warning)
                            AppendToOutput($"{message.Text}", Color.DarkOrange);
                        else if (message.MessageType == KbConstants.KbMessageType.Error)
                            AppendToOutput($"{message.Text}", Color.DarkRed);
                    }

                    if (string.IsNullOrWhiteSpace(result.ExceptionText) == false)
                    {
                        AppendToOutput($"{result.ExceptionText}", Color.DarkOrange);
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
                MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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

            TabSplitContainer.Panel2Collapsed = false;

            AppendToOutput($"Exception: {ex.Message}\r\n", Color.DarkRed);
        }

        #endregion
    }
}
