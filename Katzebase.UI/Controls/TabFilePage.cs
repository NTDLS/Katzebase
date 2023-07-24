using ICSharpCode.AvalonEdit;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Katzebase.UI.Classes;
using System.Text;

namespace Katzebase.UI.Controls
{
    internal class TabFilePage : TabPage, IDisposable
    {
        #region Properties.

        public TabControl TabControlParent { get; private set; }
        public int ExecutionExceptionCount { get; private set; } = 0;
        public bool IsScriptExecuting { get; private set; } = false;
        public string ServerAddressURL { get; set; }
        public KbClient? Client { get; private set; }
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
        public Panel ResultsPanel { get; private set; } = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
        };

        public RichTextBox OutputTextbox { get; private set; } = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font("Courier New", 10, FontStyle.Regular),
            WordWrap = false,
        };

        public TabControl BottomTabControl { get; private set; } = new() { Dock = DockStyle.Fill };
        public TextEditor Editor { get; private set; }
        public FormFindText FindTextForm { get; private set; }
        public FormReplaceText ReplaceTextForm { get; private set; }

        #endregion

        #region IDisposable.

        private bool disposed = false;
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            base.Dispose();
        }

        protected new virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                Client?.Dispose();
            }

            disposed = true;
            base.Dispose(disposing);
        }

        #endregion

        public TabFilePage(TabControl tabControlParent, string serverAddressURL, string tabText, TextEditor editor) :
             base(tabText)
        {
            TabControlParent = tabControlParent;
            Editor = editor;
            FindTextForm = new FormFindText(this);
            ReplaceTextForm = new FormReplaceText(this);
            ServerAddressURL = serverAddressURL;
            if (string.IsNullOrEmpty(serverAddressURL) == false)
            {
                Client = new KbClient(ServerAddressURL);
            }
        }

        public static TabFilePage Create(EditorFactory editorFactory, string tabText = "", string serverAddress = "")
        {
            if (string.IsNullOrWhiteSpace(tabText))
            {
                tabText = FormUtility.GetNextNewFileName();
            }

            var newInstance = editorFactory.Create(serverAddress, tabText);

            newInstance.Editor.KeyUp += newInstance.Editor_KeyUp;
            newInstance.Controls.Add(newInstance.TabSplitContainer);

            newInstance.TabSplitContainer.Panel1.Controls.Add(new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = newInstance.Editor
            });

            newInstance.TabSplitContainer.Panel2.Controls.Add(newInstance.BottomTabControl);
            newInstance.BottomTabControl.TabPages.Add(newInstance.OutputTab); //Add output tab to bottom.
            newInstance.OutputTab.Controls.Add(newInstance.OutputTextbox);
            newInstance.BottomTabControl.TabPages.Add(newInstance.ResultsTab); //Add results tab to bottom.

            newInstance.ResultsTab.Controls.Add(newInstance.ResultsPanel);

            newInstance.TabSplitContainer.SplitterMoved += TabSplitContainer_SplitterMoved;
            newInstance.TabSplitContainer.SplitterDistance = Preferences.Instance.ResultsSplitterDistance;

            newInstance.Client?.Server.Ping();
            newInstance.Editor.Focus();

            return newInstance;
        }

        private static void TabSplitContainer_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            if (sender is SplitContainer container)
            {
                Preferences.Instance.ResultsSplitterDistance = container.SplitterDistance;
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

                foreach (var dgv in ResultsPanel.Controls.OfType<DataGridView>().ToList())
                {
                    dgv.Dispose();
                }
                ResultsPanel.Controls.Clear();

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

                tabFilePage.Text = $"{tabFilePage.Text} | (executing)";

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

                bool hasResults = false;

                foreach (var dgv in ResultsPanel.Controls.OfType<DataGridView>())
                {
                    dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    hasResults = true;
                }

                if (TabControlParent.SelectedTab == tabFilePage)
                {
                    if (hasResults)
                    {
                        BottomTabControl.SelectedTab = ResultsTab;
                    }
                    else
                    {
                        BottomTabControl.SelectedTab = OutputTab;
                    }

                    tabFilePage.Focus();
                    tabFilePage.Editor.Focus();
                }

                tabFilePage.Text = tabFilePage.Text.Replace(" | (executing)", "");

                IsScriptExecuting = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        class MetricsTextItem
        {
            public string Value { get; set; } = string.Empty;
            public string Average { get; set; } = string.Empty;
            public string Count { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        private void ExecuteCurrentScriptSync(KbClient client, string scriptText, bool justExplain)
        {
            var group = new WorkloadGroup();

            try
            {
                group.OnException += Group_OnException;
                group.OnStatus += Group_OnStatus;

                var scripts = KbUtility.SplitQueryBatchesOnGO(scriptText);

                int batchNumber = 1;

                foreach (var script in scripts)
                {
                    DateTime startTime = DateTime.UtcNow;

                    KbQueryResultCollection result;

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

                        var metricsTextItems = new List<MetricsTextItem>();

                        foreach (var wt in result.Metrics.Where(o => o.Value >= 0.5).OrderBy(o => o.Value))
                        {
                            if (wt.MetricType == KbConstants.KbMetricType.Cumulative)
                            {
                                metricsTextItems.Add(new MetricsTextItem()
                                {
                                    Name = wt.Name,
                                    Value = $"Value: {wt.Value:n0}",
                                    Average = $"Average: {wt.Value / wt.Count:n2}",
                                    Count = $"Count: {wt.Count:n0}"
                                });
                            }
                            else
                            {
                                metricsTextItems.Add(new MetricsTextItem() { Name = wt.Name, Value = $"Value: {wt.Value:n0}", });
                            }
                        }

                        if (metricsTextItems.Count > 0)
                        {
                            int maxValueTength = metricsTextItems.Max(o => o.Value.Length);
                            int maxAverageTength = metricsTextItems.Max(o => o.Average.Length);
                            int maxCountTength = metricsTextItems.Max(o => o.Count.Length);

                            foreach (var metricsTextItem in metricsTextItems)
                            {
                                int diff = (maxValueTength - metricsTextItem.Value.Length) + 1;
                                string metricText = $"{metricsTextItem.Value}{new string(' ', diff)}";

                                diff = (maxCountTength - metricsTextItem.Count.Length) + 1;
                                metricText += $"{metricsTextItem.Count}{new string(' ', diff)}";

                                diff = (maxAverageTength - metricsTextItem.Average.Length) + 1;
                                metricText += $"{metricsTextItem.Average}{new string(' ', diff)}";

                                metricText += metricsTextItem.Name;

                                stringBuilder.AppendLine($"  {metricText}");
                            }
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

        private List<DataGridView> AddEvenlyDistributedDataGridViews(int numDataGridViews)
        {
            var results = new List<DataGridView>();

            foreach (var dgv in ResultsPanel.Controls.OfType<DataGridView>().ToList())
            {
                dgv.Dispose();
            }
            ResultsPanel.Controls.Clear();

            int spacing = 10;
            int totalSpacing = (numDataGridViews - 1) * spacing;
            int availableHeight = ResultsPanel.Height - totalSpacing;
            int dataGridViewTop = 0;

            int dataGridViewHeight = availableHeight / 2;

            if (dataGridViewHeight < 100)
            {
                dataGridViewHeight = 100;
            }

            for (int i = 0; i < numDataGridViews; i++)
            {
                var dataGridView = new DataGridView()
                {
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
                    Height = dataGridViewHeight
                };

                results.Add(dataGridView);

                dataGridView.Width = ResultsPanel.Width;
                dataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                if (numDataGridViews > 1)
                {
                    dataGridView.Dock = DockStyle.Top;
                }
                else
                {
                    dataGridView.Dock = DockStyle.Fill;
                }

                ResultsPanel.Controls.Add(dataGridView);

                dataGridViewTop += dataGridView.Height + spacing;
            }

            results.Reverse();

            return results;
        }

        private void PopulateResultsGrid(KbQueryResultCollection resultCollection)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<KbQueryResultCollection>(PopulateResultsGrid), resultCollection);
                return;
            }

            var results = resultCollection.Collection.Where(o => o.Rows.Any()).ToList();

            var outputGrids = AddEvenlyDistributedDataGridViews(results.Count());

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var outputGrid = outputGrids[i];

                try
                {
                    if (result == null || result.Rows.Count == 0)
                    {
                        continue;
                    }

                    outputGrid.SuspendLayout();

                    foreach (var field in result.Fields)
                    {
                        outputGrid.Columns.Add(field.Name, field.Name);
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

                        outputGrid.Rows.Add(rowValues.ToArray());

                        maxRowsToLoad--;
                        if (maxRowsToLoad <= 0)
                        {
                            break;
                        }
                    }

                    outputGrid.ResumeLayout();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", KbConstants.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
