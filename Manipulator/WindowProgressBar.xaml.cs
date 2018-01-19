using System.Windows;
using System.ComponentModel;
using System.Threading;

namespace Manipulator
{
    /// <summary>
    /// Interaction logic for WindowProgressBar.xaml
    /// </summary>
    public partial class WindowProgressBar : Window
    {
        private void ProgStart()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }

        private int m_NewValue;
        public bool Finished = false;

        public int NewValue
        {
            set { m_NewValue = value; }
        }

        private string m_Text;
        public string Text
        {
            set { m_Text = value; }
        }

        public WindowProgressBar(int barSize)
        {
            InitializeComponent();
            progBar.Maximum = barSize;
            ProgStart();
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progBar.Value = e.ProgressPercentage;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!Finished)
            {
                (sender as BackgroundWorker).ReportProgress(m_NewValue);
                Thread.Sleep(10);
            }
        }
    }
}
