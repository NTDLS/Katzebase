using Katzebase.Library.Exceptions;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace Katzebase.UI
{
    public partial class FormExecute : Form
    {
        private string _projectFile = string.Empty;
        private bool _firstShown = true;
        private readonly System.Windows.Forms.Timer _timer = new();
        private readonly WorkloadGroup _processor = new();

        public FormExecute()
        {
            InitializeComponent();
            _projectFile = string.Empty;
            Shown += FormExecute_Shown;
        }

        public FormExecute(string projectFile)
        {
            InitializeComponent();
            _projectFile = projectFile;


            Shown += FormExecute_Shown;
            FormClosing += FormExecute_FormClosing;
        }

        private void FormExecute_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_processor != null && _processor.IsRunning)
            {
                var messageBoxResult = MessageBox.Show($"Do you want to end the current process?", $"End Process", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (messageBoxResult == DialogResult.Yes)
                {
                    Stop();
                }
                e.Cancel = true;
            }
            else if (_processor != null && _processor.IsStopping)
            {
                MessageBox.Show($"Please wait while the current workload is stopped.", $"Ending ProcessProcess", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            else
            {
                //Close down shop...
            }
        }

        private void FormExecute_Load(object sender, EventArgs e)
        {
        }

        private Chart InitializeThroughputChart()
        {
            var chart = new Chart()
            {
                Name = "chart",
                Dock = DockStyle.Fill,
            };

            splitContainerBody.Panel1.Controls.Add(chart);

            var series = new Series
            {
                Name = "Threads",
                Color = Color.LightCoral,
                IsVisibleInLegend = true,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.SplineArea,
            };
            chart.Series.Add(series);

            series = new Series
            {
                Name = "Workloads",
                Color = Color.LightBlue,
                IsVisibleInLegend = true,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.SplineArea,
            };
            chart.Series.Add(series);

            series = new Series
            {
                Name = "Commands/s",
                Color = Color.Green,
                IsVisibleInLegend = true,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.Spline
            };
            chart.Series.Add(series);

            series = new Series
            {
                Name = "Records/s",
                Color = Color.Blue,
                IsVisibleInLegend = true,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.Spline
            };
            chart.Series.Add(series);

            series = new Series
            {
                Name = "Fields/s",
                Color = Color.Orange,
                IsVisibleInLegend = true,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.Spline
            };
            chart.Series.Add(series);

            ChartArea chartArea = new();
            chartArea.Name = "DefaultChartArea";
            chartArea.AxisY.IsStartedFromZero = true;
            chartArea.AxisY.MajorGrid.Enabled = true;
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.LabelStyle.Enabled = false;
            chart.ChartAreas.Add(chartArea);

            Legend legend = new()
            {
                Name = "DefaultLegend",
                DockedToChartArea = "DefaultChartArea",
                Docking = Docking.Left
            };
            chart.Legends.Add(legend);

            return chart;
        }

        private void FormExecute_Shown(object? sender, EventArgs e)
        {
            if (_firstShown)
            {
                _firstShown = false;
                Start();
                _timer.Start();
            }
        }

        private void Start()
        {
            _processor.OnException += Processor_OnException;
            _processor.OnStatus += _processor_OnStatus;
            _processor.OnStopped += _processor_OnStopped;
            _processor.Start();

            _timer.Interval = 100;
            _timer.Tick += _timer_Tick;
        }

        private void _processor_OnStopped(WorkloadGroup sender)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<WorkloadGroup>(_processor_OnStopped), sender);
                return;
            }

            AppendOutputText("Process terminated.", Color.Orange);
            this.Close();
        }

        private void Stop()
        {
            _timer.Stop();
            _processor.StopAsync();
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
        }

        void AppendOutputText(string text, Color color)
        {
            richTextBoxOutput.SelectionStart = richTextBoxOutput.TextLength;
            richTextBoxOutput.SelectionLength = 0;

            richTextBoxOutput.SelectionColor = color;
            richTextBoxOutput.AppendText($"{text}\r\n");
            richTextBoxOutput.SelectionColor = richTextBoxOutput.ForeColor;

            richTextBoxOutput.SelectionStart = richTextBoxOutput.Text.Length;
            richTextBoxOutput.ScrollToCaret();
        }

        private void _processor_OnStatus(WorkloadGroup sender, string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<WorkloadGroup, string, Color>(_processor_OnStatus), sender, text, color);
                return;
            }

            AppendOutputText(text, color);
        }

        private void Processor_OnException(WorkloadGroup sender, KbExceptionBase ex)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<WorkloadGroup, KbExceptionBase>(Processor_OnException), sender, ex);
                return;
            }

            var message = new StringBuilder();
            message.Append(ex.Message);
            AppendOutputText(message.ToString(), Color.Red);
        }
    }
}
