using System.ComponentModel;

namespace NTDLS.Katzebase.SQLServerMigration
{
    public class AbortableBackgroundWorker : BackgroundWorker
    {

        private Thread? workerThread;

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            workerThread = Thread.CurrentThread;
            try
            {
                base.OnDoWork(e);
            }
            catch (ThreadAbortException)
            {
                e.Cancel = true; //We must set Cancel property to true!
            }
        }

        public void Abort()
        {
            if (workerThread != null)
            {
                workerThread.Interrupt();
                workerThread.Join();
                workerThread = null;
            }
        }
    }
}
