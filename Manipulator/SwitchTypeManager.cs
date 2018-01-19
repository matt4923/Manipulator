using System;


namespace Manipulator
{
    internal class SwitchTypeManager
    {
        private string m_FullSwitchFile;

        public SwitchTypeManager(string fullSwitchFile)
        {
            m_FullSwitchFile = fullSwitchFile;
        }

        internal SwitchProperties.SwitchType GetSwitchTypeFromFile()
        {
            //TO DO, WORK TO FIGURE SWITCH BASED ON REPORT

            if(m_FullSwitchFile.Contains("[KCommand: disp stat"))
            {
                return SwitchProperties.SwitchType.Avaya;
            }
            else if(m_FullSwitchFile.Contains("TNB")&&m_FullSwitchFile.Contains("REQ:"))
            {
                return SwitchProperties.SwitchType.CS1000;
            }
            else
            {
                return SwitchProperties.SwitchType.NotSupported;
            }
        }
    }
}