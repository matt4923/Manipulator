using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace Manipulator
{
    internal class AvayaFileAppender : ProcessManager 
    {
        private Dictionary<string, string> m_ScriptsDictionary;
        private string m_fullSwitchFile;
        private ScriptManager m_ScriptManager;
        string[] m_SplitFullSwitchFile;
        private const char ESC = (char)27;
        private string COMMAND_TEXT_ENDING = $@"[24;1H{ESC}[KCommand:"; //add stripped stuff back
        private int m_TotalSizeOfProgBar;


        internal override int TotalSizeOfProgBar
        {
            get
            {
                return m_TotalSizeOfProgBar;
            }

            set
            {
                m_TotalSizeOfProgBar = value;
            }
        }

        public AvayaFileAppender(string fullSwitchFile, string path)
        {
            m_fullSwitchFile = fullSwitchFile;
           // ReplacedFile = m_NewSwitchFile;
            i_AppendedFilePath = path;
        }

        public AvayaFileAppender(string fullSwitchFile, ScriptManager scriptManager, string filePath) : this(fullSwitchFile, filePath)
        {
            m_ScriptManager = scriptManager;
            m_ScriptsDictionary = scriptManager.ScriptsDictionary;
            try {
                fullSwitchFile = fullSwitchFile.Replace($"{ESC}[6~", string.Empty);
                fullSwitchFile = fullSwitchFile.Replace($"{ESC}[3~", string.Empty);
                m_SplitFullSwitchFile = ParseAvayaFile(fullSwitchFile);
                if (m_SplitFullSwitchFile == null)
                {
                    throw new Exception("Issue parsing out Avaya type file.");
                }
            }catch(Exception ex) {
                Logger.WriteLog(ex.Message);
                MessageBox.Show(ex.Message);
            }
            Process();
        }


        private string[] ParseAvayaFile(string fullSwitchFile)
        {
            string[] allRecords;
            if (Regex.IsMatch(fullSwitchFile, @"\[24;1H\e\[KCommand:"))
            {
                allRecords = Regex.Split(fullSwitchFile, @"\[24;1H\e\[KCommand:");

                TotalSizeOfProgBar = allRecords.Length;
            }
            else { allRecords = null; }
            
            return allRecords;
        }

        //public string ReplacedFile
        //{
        //    get { return m_NewSwitchFile; }
        //    set
        //    {
        //        m_NewSwitchFile += value;
        //    }
        //} 

        //private string m_NewSwitchFile = "";
        internal override void DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                int i = 0;
                foreach(string record in m_SplitFullSwitchFile)
                {
                    ProgBar.NewValue = i;
                    i += 1;

                    string newRecord = record; 
                    if (!Regex.IsMatch(newRecord, @"^\sdisp stat")) { AppendToNewReplacedFile(newRecord + COMMAND_TEXT_ENDING); continue; }
                    //New Display record
                    newRecord = ProcessRecord(newRecord);
                    string dataToAdd = newRecord + COMMAND_TEXT_ENDING;
                    if (dataToAdd.Trim() == string.Empty) {
                        continue;
                    }
                    if (!AppendToNewReplacedFile(dataToAdd))
                    {
                         throw new Exception($"Error adding to new file. Record:/n/n{dataToAdd}");
                    }
                }
            }
            catch (Exception ex) {
                Logger.WriteLog(ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        

        private string m_FieldNamePattern = @"^\[(?<row>\d{1,2})[^;]*;\d{1,2}H\s*(?<fieldName>[\040-\071\073-\176]+)[?:]\s$";
        private string m_ValuePattern = @"^\[(?<row>\d{1,2})[^;]*;\d{1,2}H\s*(?<value>[\040-\176]+)";
        private string m_FieldValuePattern = @"^\[(?<row>\d{1,2})[^;]*;\d{1,2}H\s*(?<fieldName>[\040-\071\073-\176]+)[?:]\s(?<value>[\040-\176]+)$";

        private string ProcessRecord(string record)
        {
            string DispRecord = "";
            string newFieldKey = string.Empty;
            string newRecordString = "";
            bool changeValue = false;
            string[] parts = Regex.Split(record, @"\e");
            DispRecord = parts[0];

            int p = 0;
            for (p = 0; p <= (parts.Length - 1); p++)
            {
                string piece = parts[p];
                if (Regex.IsMatch(piece, m_FieldNamePattern))
                {
                    //FIELD NAME
                    changeValue = AvayaFieldCheckIfValueChanges(piece, ref newFieldKey);
                }
                else if (Regex.IsMatch(piece, m_ValuePattern))
                {
                    if (IsValueActuallyField(piece)&&!changeValue)
                    {
                        changeValue = AvayaFieldCheckIfValueChanges_WasValue(piece, ref newFieldKey);
                        newRecordString += piece + ESC;
                        continue;
                    }
                    //VALUE
                    if (changeValue)
                    {
                        piece = HandleValueChange(piece, newFieldKey);
                    } 
                       
                    newFieldKey = string.Empty;
                    changeValue = false;
                }
                else if (Regex.IsMatch(piece, m_FieldValuePattern))
                {
                    //FIELD NAME AND VALUE TOGETHER -- cairs comment, haven't seen this situation yet.

                    string str = "";
                }

                newRecordString += piece + ESC;

                if (parts[p].Contains("Command aborted"))
                {
                    break; 
                }
            }
            return newRecordString;
        }

        private bool IsValueActuallyField(string piece)
        {
            var stringCheck = piece.ToUpper();
            if (stringCheck.Contains("EXT:")) { return true; }
            else if (stringCheck.Contains("E:")) { return true; }
            //else if (stringCheck.Contains("EXT:")) { return true; }
            else { return false; }

        }

        private string HandleValueChange(string piece, string FieldKey)
        {
            Match m = Regex.Match(piece, m_ValuePattern);
            string value = m.Groups["value"].Value.Trim();

            string newDataToAdd = m_ScriptsDictionary[FieldKey];
            ScriptNewValueManager svm = new ScriptNewValueManager(newDataToAdd, value);

            string pieceCheck = value;

            if (FieldKey == SwitchProperties.AvayaFields.Extension.ToString().ToUpper() || FieldKey == "E" || FieldKey == "EXT")
            {
                pieceCheck = pieceCheck.Replace("-", "");
            } 

            if (svm.PassesConditions(pieceCheck))
            {
                return piece.Replace(value, svm.NewValue.Trim());
            }
            else
            {
                return piece;
            }
        }

        private bool AvayaFieldCheckIfValueChanges_WasValue(string piece, ref string newFieldKey)
        {
            Match m = Regex.Match(piece, m_ValuePattern);
            string fieldName = m.Groups["value"].Value.Trim().Replace(":", "");
            if (m_ScriptsDictionary.ContainsKey(fieldName.ToUpper()))
            {
                newFieldKey = fieldName.ToUpper();
                return true;
            }
            return false;
        }

        private bool AvayaFieldCheckIfValueChanges(string piece, ref string newFieldKey)
        {
            Match m = Regex.Match(piece, m_FieldNamePattern);
            string fieldName = m.Groups["fieldName"].Value.Trim();
            if (m_ScriptsDictionary.ContainsKey(fieldName.ToUpper()))
            {
                newFieldKey = fieldName.ToUpper();
                return true;
            }
            return false;
        }
    }
}
