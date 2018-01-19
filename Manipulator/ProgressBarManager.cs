using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Manipulator 
{
    abstract class ProgressBarManager //NOT NEEDED???
    {
        WindowProgressBar m_ProgBar;
        internal void StartProgressBar(int maxProgress)
        {
            m_ProgBar = new WindowProgressBar(maxProgress);
            m_ProgBar.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            m_ProgBar.Show();
        }

        internal void StopProgressBar()
        {
            m_ProgBar.Finished = true;
            m_ProgBar.Close();
        }

        internal void UpdateProgressBarValue(int newVal)
        {
            m_ProgBar.NewValue = newVal;
        }

    }
}
