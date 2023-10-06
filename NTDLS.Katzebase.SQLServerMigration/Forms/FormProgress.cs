namespace NTDLS.Katzebase.SQLServerMigration
{
    public partial class FormProgress : Form
    {
        #region Events

        public class OnCancelInfo
        {
            public bool Cancel = false;
        }

        public delegate void EventOnCancel(object sender, OnCancelInfo e);
        public event EventOnCancel? OnCancel;

        #endregion

        #region Singleton

        public static class Singleton
        {
            private const string LockObject = "FormProgress.Singleton.LockObject";

            private static FormProgress? singleton = null;
            public static FormProgress Form
            {
                get
                {
                    lock (LockObject)
                    {
                        if (singleton == null)
                        {
                            singleton = new FormProgress();
                        }
                    }
                    return singleton;
                }
            }

            public static DialogResult ShowNew(string titleText)
            {
                lock (LockObject)
                {
                    if (singleton != null)
                    {
                        singleton.Dispose();
                        singleton = null;
                    }

                    singleton = new FormProgress();
                    singleton.SetTitleText(titleText);
                }

                return singleton.ShowDialog();
            }

            public static DialogResult ShowNew(string titleText, string headerText, string bodyText)
            {
                lock (LockObject)
                {
                    if (singleton != null)
                    {
                        singleton.Dispose();
                        singleton = null;
                    }

                    singleton = new FormProgress();
                    singleton.SetTitleText(titleText);
                    singleton.SetHeaderText(headerText);
                    singleton.SetBodyText(bodyText);
                }

                return singleton.ShowDialog();
            }

            public static DialogResult ShowNew(string headerText, string bodyText)
            {
                lock (LockObject)
                {
                    if (singleton != null)
                    {
                        singleton.Dispose();
                        singleton = null;
                    }

                    singleton = new FormProgress();
                    singleton.SetHeaderText(headerText);
                    singleton.SetBodyText(bodyText);
                }

                return singleton.ShowDialog();
            }

            public static DialogResult ShowNew(string headerText, EventOnCancel onCancel)
            {
                lock (LockObject)
                {
                    if (singleton != null)
                    {
                        singleton.Dispose();
                        singleton = null;
                    }

                    singleton = new FormProgress();
                    singleton.SetHeaderText(headerText);
                    singleton.OnCancel += onCancel;
                    singleton.SetCanCancel(true);
                }

                return singleton.ShowDialog();
            }

            public static DialogResult ShowNew(string headerText, string bodyText, EventOnCancel onCancel)
            {
                lock (LockObject)
                {
                    if (singleton != null)
                    {
                        singleton.Dispose();
                        singleton = null;
                    }

                    singleton = new FormProgress();
                    singleton.OnCancel += onCancel;
                    singleton.SetHeaderText(headerText);
                    singleton.SetBodyText(bodyText);
                    singleton.SetCanCancel(true);
                }

                return singleton.ShowDialog();
            }

            public static void WaitForVisible()
            {
                while (true)
                {
                    lock (LockObject)
                    {
                        if (singleton != null && singleton.HasBeenShown == true)
                        {
                            break;
                        }
                    }
                    Thread.Sleep(10);
                }
            }

            public static void Close(DialogResult result)
            {
                lock (LockObject)
                {
                    singleton?.Close(result);
                }
            }

            public static void Close()
            {
                lock (LockObject)
                {
                    singleton?.Close();
                }
            }
        }

        #endregion

        /// <summary>
        /// Used by the user to set proprietary state information;
        /// </summary>
        public object? UserData { get; set; } = null;
        public bool HasBeenShown { get; private set; } = false;
        public bool IsCancelPending { get; private set; } = false;

        public FormProgress()
        {
            InitializeComponent();

            lblBody.Text = "";
            buttonCancel.Enabled = false;
            pbProgress.Minimum = 0;
            pbProgress.Maximum = 100;



            DialogResult = DialogResult.OK;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            IsCancelPending = true;
            if (OnCancel != null)
            {
                OnCancelInfo onCancelInfo = new OnCancelInfo();
                OnCancel(this, onCancelInfo);
                if (onCancelInfo.Cancel)
                {
                    return;
                }
            }
        }

        public new void Close()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(Close));
                return;
            }

            base.Close();
        }

        public void Close(DialogResult result)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DialogResult>(Close), result);
                return;
            }

            DialogResult = result;

            base.Close();
        }

        public void SetHeaderText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetHeaderText), text);
                return;
            }

            lblHeader.Text = text;
        }

        public void SetBodyText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetBodyText), text);
                return;
            }

            lblBody.Text = text;
        }

        public void SetTitleText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetTitleText), text);
                return;
            }

            Text = text;
        }

        public void SetProgressMinimum(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetProgressMinimum), value);
                return;
            }

            if (pbProgress.Style == ProgressBarStyle.Marquee)
            {
                pbProgress.Style = ProgressBarStyle.Continuous;
            }

            pbProgress.Minimum = value;
        }

        public void SetProgressMaximum(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetProgressMaximum), value);
                return;
            }

            if (pbProgress.Style == ProgressBarStyle.Marquee)
            {
                pbProgress.Style = ProgressBarStyle.Continuous;
            }

            pbProgress.Maximum = value;
        }

        public void IncrementProgressValue()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(IncrementProgressValue));
                return;
            }

            if (pbProgress.Style == ProgressBarStyle.Marquee)
            {
                pbProgress.Style = ProgressBarStyle.Continuous;
            }

            pbProgress.Value++;
        }

        public void SetProgressValue(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetProgressValue), value);
                return;
            }

            if (pbProgress.Style == ProgressBarStyle.Marquee)
            {
                pbProgress.Style = ProgressBarStyle.Continuous;
            }

            pbProgress.Value = value;
        }

        public void SeProgressStyle(ProgressBarStyle value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<ProgressBarStyle>(SeProgressStyle), value);
                return;
            }

            pbProgress.Style = value;
        }

        public void SetCanCancel(bool value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetCanCancel), value);
                return;
            }

            buttonCancel.Enabled = value;
        }

        private void FormProgress_Shown(object sender, EventArgs e)
        {
            HasBeenShown = true;
            IsCancelPending = false;
        }
    }
}
