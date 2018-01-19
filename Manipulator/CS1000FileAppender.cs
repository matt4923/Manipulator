using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace Manipulator
{
    internal class CS1000FileAppender : ProcessManager 
    {
        const string KEY = "KEY";
        const string CLS = "CLS";
        private string fullSwitchFile;
        private ScriptManager m_ScriptManager;
        private Dictionary<string, string> m_ScriptsDictionary;
       
        string[] m_SplitFullSwitchFile;

        public CS1000FileAppender(string fullSwitchFile, ScriptManager scriptManager, string AppendedFilePath)
        {
            this.fullSwitchFile = fullSwitchFile;
            
            i_AppendedFilePath = AppendedFilePath;

            m_ScriptManager = scriptManager;
            m_ScriptsDictionary = scriptManager.ScriptsDictionary;
            try
            {
                
                m_SplitFullSwitchFile = ParseCS1000File(fullSwitchFile);
                if (m_SplitFullSwitchFile == null)
                {
                    throw new Exception("Issue parsing out Avaya type file.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message);
                MessageBox.Show(ex.Message);
            }
            Process();
        }

        string m_Delimeter;
        private string[] ParseCS1000File(string fullSwitchFile)
        {
            string[] allRecords;
            m_Delimeter = string.Empty;

            fullSwitchFile = fullSwitchFile.Replace("DES  ", "DESDES  ");

            if(Regex.IsMatch(fullSwitchFile, "\r\n\r\nDES"))
            {
                allRecords = Regex.Split(fullSwitchFile, "\r\n\r\nDES");
                m_Delimeter = "\r\n"; 
            }
            else if(Regex.IsMatch(fullSwitchFile, "\n\r\n\rDES"))
            {
                allRecords = Regex.Split(fullSwitchFile, "\n\r\n\rDES");
                m_Delimeter = "\n\r";
            }
            else if (Regex.IsMatch(fullSwitchFile, "\n\nDES"))
            {
                allRecords = Regex.Split(fullSwitchFile, "\n\nDES");
                m_Delimeter = "\n";
            }
            else if (Regex.IsMatch(fullSwitchFile, "\r\r\nDES"))
            {
                allRecords = Regex.Split(fullSwitchFile, "\r\r\n\r\r\nDES");
                m_Delimeter = "\r\r\n";
            }
            else
            {
                fullSwitchFile = fullSwitchFile.Replace("DESDES  ", "DES  ");
                allRecords = Regex.Split(fullSwitchFile, "\r\r");
                m_Delimeter = "\r";
            }

            TotalSizeOfProgBar = allRecords.Length;

            return allRecords;
        }

        internal override void DoWork(object sender, DoWorkEventArgs e)
        {
            

            try
            {
                int i = 0;
                foreach (string record in m_SplitFullSwitchFile)
                {
                    ProgBar.NewValue = i;
                    i += 1;

                    
                    if (!Regex.IsMatch(record, m_Delimeter + @"DN\s") && !Regex.IsMatch(record, m_Delimeter + @"KEY\s"))
                    {
                        AppendToNewReplacedFile(record + m_Delimeter+m_Delimeter);
                        continue;
                    }
                    
                    string newRecord = ProcessRecord(record);
                    
                    if (newRecord.Trim() == string.Empty)
                    {
                        continue;
                    }
                    if (!AppendToNewReplacedFile(newRecord + m_Delimeter))
                    {
                        throw new Exception($"Error adding to new file. Record:/n/n{newRecord}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        private string ProcessRecord(string record)
        {
            string[] recLines = Regex.Split(record, m_Delimeter);
            string newRec = string.Empty;
            string lastField = string.Empty;
            //foreach (string line in recLines)
            for(int i = 0; i <= recLines.Length - 1;i++)
            {
                string line = recLines[i];
                string newLine = line;
                string[] lineSplit = line.Split(' ');
                string field = lineSplit[0];
                if (field == CLS)
                {
                    i = HandleClsField(i, recLines, ref newRec);
                    continue;
                }

                if (field == KEY)
                {
                    i = HandleKeyField(i, recLines, ref newRec);
                    continue;
                }

                string value = string.Empty;
                if (lineSplit.Length > 1 && lastField != SwitchProperties.CS1000Fields.DN.ToString())
                {
                    value = line.Replace(field, string.Empty).Trim();
                }
                
                if (FieldValueChanges(field.Trim()))
                {
                    //There is a script for this field.  Possible change.

                    if(field.Trim() == SwitchProperties.CS1000Fields.DN.ToString())
                    {
                        //get just the TN not the MARP or extra data... this is not hit on the KEY fields (digital) only analog DN
                        value = value.Trim().Split(' ')[0];
                    }
                    string newVal = HandleFieldChangeValue(field.Trim(), value);
                    newLine = newLine.Replace(value, newVal);
                }
               
                newRec += newLine + m_Delimeter;
                if(field.Trim() != string.Empty)
                {
                    lastField = field;
                }
            }

            return newRec;
        }

        private int HandleClsField(int curIndx, string[] recLines, ref string newRec)
        {
            int i = curIndx;
            bool finishedClsFields = false;
            while (!finishedClsFields)
            {
                string line = recLines[i];
                string[] lineSplit = line.Split(' ');
                string field = lineSplit[0];
                if (field.Trim()== CLS || field.Trim() == string.Empty)
                {
                    newRec += line + m_Delimeter;
                    i += 1;
                }
                else {
                    finishedClsFields = true;
                }
            }
            
            return i - 1;
        }

        private int HandleKeyField(int curIndx, string[] recLines, ref string newRec)
        {
            string KeyFieldPattern1 = @"((KEY\s{2})|(\s{5}))(?<KEY>\d{2})\s(?<KEY_DIAL_TONE_FEATURE>\D{3})\s(?<DN>\d{1,10})\s{1}(?<CLID>\w+)\s*$";
            string KeyFieldPattern2 = @"((KEY\s{2})|(\s{5}))(?<KEY>\d{2})\s(?<KEY_DIAL_TONE_FEATURE>\D{3})\s(?<DN>\d{1,10})\s{1}(?<CLID>\w+)\s{3,}(?<MADN>MARP)$";
            string KeyFieldPattern3 = @"((KEY\s{2})|(\s{5}))(?<KEY>\d{2})\s(?<KEY_DIAL_TONE_FEATURE>ACD)\s(?<ACD_PILOT>\d{1,10})\s{1}(?<CLID>\w+)\s{2,}(?<DN>\d{1,10})$";
            string KeyFieldPattern4 = @"\s{5}(?<KEY>[0-9]){2}\s[DIG]{3}\s(?<INT_GRP>[0-9]+)\s(?<INT_GRP_DATA>[0-9]+\s[V,R]?)";

            int i = curIndx;
            bool finishedKeyFields = false;

            while (!finishedKeyFields)
            {
                string line = recLines[i];
                string[] lineSplit = line.Split(' ');
                string field = lineSplit[0];
                Match m;
                
                if (field.Trim() == KEY || field.Trim() == string.Empty)
                {
                    if(Regex.IsMatch(line, KeyFieldPattern1))
                    {
                        //button 1 with SCR (KEY  00 SCR 76770 0 )
                        m = Regex.Match(line, KeyFieldPattern1);
                        Regex grpNames = new Regex(KeyFieldPattern1);
                        line = CheckGroupsForChange(m, line, grpNames);        
                    }
                    else if (Regex.IsMatch(line, KeyFieldPattern2))
                    {
                        //Keys with MARP (KEY  00 SCR 76089 0     MARP)
                        m = Regex.Match(line, KeyFieldPattern2);
                        Regex grpNames = new Regex(KeyFieldPattern2);
                        line = CheckGroupsForChange(m, line, grpNames);
                    }
                    else if (Regex.IsMatch(line, KeyFieldPattern3))
                    {
                        //ACD's (KEY  00 ACD 75902 0  45902)
                        m = Regex.Match(line, KeyFieldPattern3);
                        Regex grpNames = new Regex(KeyFieldPattern3);
                        line = CheckGroupsForChange(m, line, grpNames);
                    }
                    else if (Regex.IsMatch(line, KeyFieldPattern4))
                    {
                        //INT_grp buttons (     13 DIG 38 26 V)
                        m = Regex.Match(line, KeyFieldPattern4);
                        Regex grpNames = new Regex(KeyFieldPattern4);
                        line = CheckGroupsForChange(m, line, grpNames);
                    }
                    else
                    {
                        //this is an indented field like CPND, XPLN, DISPLAY_FMT and all Key Features.  TO Do: handle these by getting the field and checking the value
                        string here = line;
                    }

                    i += 1;
                }
                else {
                    finishedKeyFields = true;
                }
                newRec += line + m_Delimeter;
            }
            return i;
        }

        private string CheckGroupsForChange(Match m, string line, Regex grppNames)
        {
            string[] groupNames = grppNames.GetGroupNames(); 
            string newLine = line;
            foreach (string group in groupNames)
            {
                string grpVal = m.Groups[group].Value;
                if (m_ScriptsDictionary.ContainsKey(group.ToUpper()))
                {
                    //Field that changes!
                    string newValue = HandleFieldChangeValue(group, grpVal);
                    newLine = newLine.Replace(grpVal, newValue);
                }
            }
            return newLine;
        }

        private string HandleFieldChangeValue(string field, string value)
        {
            ScriptNewValueManager svm = new ScriptNewValueManager(m_ScriptsDictionary[field], value);

            string pieceCheck = value;

            if (field == SwitchProperties.CS1000Fields.DN.ToString().ToUpper())
            {
                pieceCheck = pieceCheck.Replace("-", "");
            }

            if (svm.PassesConditions(pieceCheck))
            {
                return svm.NewValue.Trim();
            }
            else
            {
                return value;
            }
        }

        private bool FieldValueChanges(string field)
        {
            return m_ScriptsDictionary.ContainsKey(field.ToUpper());
        }

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
    }
}