using Microsoft.Win32;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Manipulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string SCRIPT_FILE_TEXT = "ManiScripts.scp";
        const string MISSING_MANISCRIPTS_MESSAGE = "***No ManiScripts.scp file found.  Create one or add \\Manipulator\\ManiScripts.scp path.***";
        string m_ScriptsFilePath = $"Manipulator\\{ SCRIPT_FILE_TEXT}";

        public MainWindow()
        {
            InitializeComponent();
            
        }
        public bool m_ProcessingOnStartup = false;
        public string DefaultSwitchFileLocation = string.Empty;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_ProcessingOnStartup = Properties.Settings.Default.StartProcessingOnStartup;
            DefaultSwitchFileLocation = Properties.Settings.Default.SwitchFileLocation;
            string scriptsPath = GetSettingsPathThenLoadExistingScripts(DefaultSwitchFileLocation, m_ScriptsFilePath); //throw this inside of a sub folder in the switch reports folder.  This is because as a service it looks under the system32 folder.
            LoadExistingScripts(scriptsPath);
            txtPath.Text = DefaultSwitchFileLocation;
            if (m_ProcessingOnStartup) { GoClick(); }
        }

        private void ShowVersion(object sender, RoutedEventArgs e)
        {
            Version ver = Assembly.GetEntryAssembly().GetName().Version;
            MessageBox.Show($"Version: {ver.ToString()}");            

        }

        private void MenuItemLoadScripts_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                LoadExistingScripts(ofd.FileName);
            }
        }

        private void LoadExistingScripts(String FilePath)
        {
            if (File.Exists(FilePath))
            {
                StreamReader sr = new StreamReader(FilePath);
                txtScript.Text = sr.ReadToEnd();
            }
            else
            {
                if (File.Exists(SCRIPT_FILE_TEXT))
                {
                    StreamReader sr = new StreamReader(SCRIPT_FILE_TEXT);
                    txtScript.Text = sr.ReadToEnd();
                }
                else
                {
                    txtScript.Text = "\n\n" + MISSING_MANISCRIPTS_MESSAGE;
                }
            }
        }


        private string GetSettingsPathThenLoadExistingScripts(string LocationWithOldFileName, string SettingsFileName)
        {
            //since I'm using the same base location as the SwitchFileLocation and to avoid adding another setting, I'm just stripping the filename out and replacing it with the settings filename
            string scriptPath = string.Empty;
            string[] oldPathSplit = LocationWithOldFileName.Split('\\');
            scriptPath = LocationWithOldFileName.Replace(oldPathSplit[oldPathSplit.Length - 1], SettingsFileName);
            return scriptPath;
        }

        private void MenuItemSaveScripts_Click(object sender, RoutedEventArgs e)
        {
            string scripts = txtScript.Text.Trim();
            if (scripts != string.Empty)
            {
                string writeToLocation = SCRIPT_FILE_TEXT;
                string testPath = GetSettingsPathThenLoadExistingScripts(DefaultSwitchFileLocation, m_ScriptsFilePath); 
                if (File.Exists(testPath) || Directory.Exists(testPath.Replace(SCRIPT_FILE_TEXT, string.Empty))){
                    writeToLocation = testPath; 
                }
                try
                {
                    File.WriteAllText(writeToLocation, scripts);
                    MessageBox.Show($"File saved Successfully!\n\n {writeToLocation}");
                }catch(Exception ex) { MessageBox.Show(ex.Message); }


            }
            else { MessageBox.Show("Please input scripts and then save."); }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private StreamReader switchFile;
        private string m_Path; 
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            if (fd.ShowDialog() == true)
            {                
                txtPath.Text = fd.FileName;
            }

        }

        private void GoClick()
        {
            m_Path = txtPath.Text.Trim();
            if (txtScript.Text.Replace(MISSING_MANISCRIPTS_MESSAGE, string.Empty).Trim() == string.Empty)
            {
                MessageBox.Show("Please input scripts, save them and retry.");
                return;
            }
            if (File.Exists(m_Path))
            {
                switchFile = new StreamReader(m_Path);
            }

            if (m_Path == string.Empty)
            {
                MessageBox.Show("Please specify data to add to file.");
            }
            else if (this.switchFile != null)
            {
                try
                {
                    SetupAndStartProcessing();
                }
                catch (Exception ex) {
                    Logger.WriteLog(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {

                MessageBox.Show("Please select an Avaya_Sets.txt file to add data too.");
            }
        }
        
        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            GoClick();
        }

        SwitchProperties.SwitchType m_SwitchReportType;
        private void SetupAndStartProcessing()
        {
            string fullSwitchFile = switchFile.ReadToEnd();
            SwitchTypeManager stm = new SwitchTypeManager(fullSwitchFile);
            m_SwitchReportType = stm.GetSwitchTypeFromFile();
            
            ScriptManager scriptManager = new ScriptManager(txtScript.Text.Trim(), m_SwitchReportType);

            string appendedFilePath = m_Path.Replace(".txt", "_Appended.txt");
            switch (m_SwitchReportType)
            {
                case SwitchProperties.SwitchType.Avaya:
                    AvayaFileAppender m_AvayaProcessor = new AvayaFileAppender(fullSwitchFile, scriptManager, appendedFilePath);
                    m_AvayaProcessor.WorkComplete += NewSwitchFileComplete;
                    break;
                case SwitchProperties.SwitchType.CS1000:
                    CS1000FileAppender m_Cs1000Processor = new CS1000FileAppender(fullSwitchFile, scriptManager, appendedFilePath);
                    m_Cs1000Processor.WorkComplete += NewSwitchFileComplete;
                    break;
                default:
                    string exception = "Switch type could not be determined by the file.";
                    Logger.WriteLog(exception);
                    throw new Exception(exception);
            }

        }

        private void NewSwitchFileComplete(object sender, ProcessManager.WorkCompleteEventArgs e)
        {
            object report;
            if(m_SwitchReportType == SwitchProperties.SwitchType.Avaya)
            {
                report = (AvayaFileAppender)sender;
            }
            else if(m_SwitchReportType == SwitchProperties.SwitchType.CS1000){
                report = (CS1000FileAppender)sender;
            } 
                
            Logger.WriteLog("New Appended File Created Successfully!");
           
            if (m_ProcessingOnStartup) { this.Close(); }
        }

        private void txtScript_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (txtScript.Text.Contains(MISSING_MANISCRIPTS_MESSAGE))
            {
                txtScript.Text =  string.Empty;
            }
        }
    }
}
