using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Manipulator
{
    abstract class ProcessManager
    {
        internal abstract void DoWork(object sender, DoWorkEventArgs e);
        private BackgroundWorker bw;
        internal WindowProgressBar ProgBar;

        public class WorkCompleteEventArgs : System.EventArgs
        {
            //can add additional parameters to send here

        }

        public delegate void WorkCompleteEventHandler(object sender, WorkCompleteEventArgs e);
        public event WorkCompleteEventHandler WorkComplete;
        private void OnWorkComplete()
        {
            if (WorkComplete != null)
            {
                WorkComplete(this, new WorkCompleteEventArgs());
            }
        }

        private void FinishedWork(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgBar.Finished = true;
            OnWorkComplete();
            ProgBar.Close();
        }

        internal abstract int TotalSizeOfProgBar { get; set; }
        internal void Process()
        {
            if (File.Exists(i_AppendedFilePath))
            {
                try
                {
                    File.Delete(i_AppendedFilePath);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Error removing old file: /n/n{ex.Message}");
                }
            }

            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FinishedWork);
            ProgBar = new WindowProgressBar(TotalSizeOfProgBar);
            ProgBar.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            ProgBar.Show();
            bw.RunWorkerAsync();
        }

        internal string i_AppendedFilePath;
        internal bool AppendToNewReplacedFile(string newText)
        {
            try
            {
                if (!File.Exists(i_AppendedFilePath))
                {
                    using (StreamWriter writer = File.CreateText(i_AppendedFilePath))
                    {
                        writer.Write(newText);
                    }
                }
                else
                {
                    using (StreamWriter writer = File.AppendText(i_AppendedFilePath))
                    {
                        writer.Write(newText);
                    }
                }
            }
            catch (Exception ex)
            {
                //just handle
                return false;
            }
            return true;
        }
    }
}
