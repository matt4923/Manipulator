using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;


namespace Manipulator
{
    internal class ScriptManager 
    {
        private string m_Scripts;
        SwitchProperties.SwitchType m_FileSwitchType;
        

    public enum ValueAppendTo
        {
            BEGINNING,
            ENDING,
            OVERWRITE,
            REPLACE,
            INVALID
        }

    public ScriptManager(string scriptText, SwitchProperties.SwitchType SwitchType)
        {
            m_Scripts = scriptText;
            m_FileSwitchType = SwitchType;
            ParseScripts();
        }


        internal void ParseScripts()
        {
            try {    
                switch (m_FileSwitchType)
                {
                    case SwitchProperties.SwitchType.Avaya:
                        ParseScriptsAvaya();
                        break;
                    case SwitchProperties.SwitchType.CS1000:
                        ParseScriptsCS1000();
                        break;
                    default:
                        throw new Exception("File not handled at this time.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        private void ParseScriptsAvaya()
        {
            string[] scriptsSplit = Regex.Split(m_Scripts, ";");
            BuildDictionary(scriptsSplit);
        }

        private void ParseScriptsCS1000()
        {
            string[] scriptsSplit = Regex.Split(m_Scripts, ";");
            BuildDictionary(scriptsSplit);
        }

        public Dictionary<string, string> ScriptsDictionary;

        private void BuildDictionary(string[] scriptsSplit)
        {
            ScriptsDictionary = new Dictionary<string, string>();
            foreach (string script in scriptsSplit)
            {
                if (script.Trim() != string.Empty)
                {
                    string[] sSplit = Regex.Split(script, ":");
                    if (sSplit.Length < 2)
                    {
                        throw new Exception($"Incorrect script format.  See line:\n{ script.ToString() }");
                    }
                    string key = sSplit[0].ToString().ToUpper().Replace("\r\n", "");
                    string value = sSplit[1].ToString();

                    if (ScriptsDictionary.ContainsKey(key)){
                        //This is an additional condition for the same field, append to existing value
                        ScriptsDictionary[key] = $"{ScriptsDictionary[key]}~{value}";
                    }
                    else {
                        ScriptsDictionary.Add(key, value);
                    }
                    
                }
            }
        }
    }

    public class ScriptNewValueManager
    {
        const string REPLACE = "REPLACE";
        private string m_ValueDataToParse;
        private ScriptManager.ValueAppendTo m_appendToPosition;
        private string m_OldValue;
        private ScriptCondition[] m_ConditionList;
        private ScriptAddtionalParameters[] m_AdditionalParametersList;
        string[] m_ConditionSplit;

        string m_NewValue =string.Empty;
        string m_NewReplaceValue = string.Empty;

        public string NewValue //NEED TO GET THE CORRECT NEW VALUE HERE BASED OFF THE CONDITION.  IT NEEDS TO LOOK UP THE NEW VALUE NOW INSTEAD OF USING WHAT'S DONE IN LINE 140
        {
            get {
                    switch (m_appendToPosition)
                    {
                        case ScriptManager.ValueAppendTo.BEGINNING:
                            return m_NewValue + m_OldValue;
                        case ScriptManager.ValueAppendTo.ENDING:
                            return m_OldValue + m_NewValue;
                        case ScriptManager.ValueAppendTo.OVERWRITE:
                            return m_NewValue;
                        case ScriptManager.ValueAppendTo.REPLACE:
                            if (m_NewReplaceValue != string.Empty)
                            {
                                return m_OldValue.Replace(m_NewReplaceValue, m_NewValue);
                            }
                            else
                            {
                                throw new Exception("Replace option with no replace text.  Please add 'replace' parameter to script.");
                            }
                        
                        default:
                            //shouldn't hit here, error caught earlier
                            return null;
                    }
                }
        }
        public object AppendTo
        {
            get { return m_appendToPosition; }
        }

        public ScriptNewValueManager(string scriptForField, string oldVal)
        {
            m_ValueDataToParse = scriptForField;
            m_OldValue = oldVal;
            m_ConditionSplit = Regex.Split(m_ValueDataToParse, "~");
            m_ConditionList = new ScriptCondition[m_ConditionSplit.Length];
            m_AdditionalParametersList = new ScriptAddtionalParameters[m_ConditionSplit.Length];
            ParseScriptValue();
        }

        
        private void ParseScriptValue()
        {

            //foreach (string condition in conditionSplit)
            for(int i = 0; i<m_ConditionSplit.Length; i++)
            {
                string condition = m_ConditionSplit[i];
                m_AdditionalParametersList[i] = null;
                string[] valueSplit = Regex.Split(condition, ",");
                if (valueSplit.Length < 2) { throw new Exception($"Problem with script format, please see line: \n{valueSplit[0]}"); }
                //m_NewValue = valueSplit[0];
                m_appendToPosition = GetValueAppendToLocation(valueSplit[1]);
                if (m_appendToPosition == ScriptManager.ValueAppendTo.INVALID) { throw new Exception($"Problem finding append to location in script. Please see line: \n{m_ValueDataToParse}"); }

                if (valueSplit.Length > 2)
                {
                    //CONDITIONS
                    string newValue = valueSplit[0];
                    string conditionsOnly = valueSplit[2];

                    m_ConditionList[i] = new ScriptCondition(conditionsOnly, newValue);

                    if (valueSplit.Length > 3)
                    {
                        //there are additional parameters that need to be used (ie. replace)
                        m_AdditionalParametersList[i] = new ScriptAddtionalParameters(BuildAddiionalParamsDictionary(valueSplit[3]));
                    }
                }
            }
        }

        private Dictionary<string, string> BuildAddiionalParamsDictionary(string additionalParams)
        {
            string[] paramsList = additionalParams.Split('&');
            Dictionary<string, string> AdditionalParamsDict = new Dictionary<string, string>();
            foreach (string param in paramsList)
            {
                string[] keyAndValueSplit = param.Split('=');
                if(keyAndValueSplit.Length < 2) { throw new Exception($"Problem with additional parameters in script.  Please see line: /n{additionalParams}"); }
                AdditionalParamsDict.Add(keyAndValueSplit[0].ToUpper(), keyAndValueSplit[1]);
            }
            return AdditionalParamsDict;
        }

        private ScriptManager.ValueAppendTo GetValueAppendToLocation(string appendId)
        {
            switch (appendId.ToUpper().Trim())
            {
                case "B":
                    return ScriptManager.ValueAppendTo.BEGINNING;
                case "E":
                    return ScriptManager.ValueAppendTo.ENDING;
                case "O":
                    return ScriptManager.ValueAppendTo.OVERWRITE;
                case "R":
                    return ScriptManager.ValueAppendTo.REPLACE;
                default:
                    return ScriptManager.ValueAppendTo.INVALID;
            }
        }

        internal bool PassesConditions(string oldPiece)
        {
            foreach(ScriptCondition cond in m_ConditionList)
            {
                if (cond.TestCondition(oldPiece)) {
                    m_NewValue = cond.PossibleNewValue;

                    int idx = Array.IndexOf(m_ConditionList, cond);
                    if (m_AdditionalParametersList[idx] != null && m_AdditionalParametersList[idx].ContainsK(REPLACE))
                    {
                        m_NewReplaceValue = m_AdditionalParametersList[idx].Value(REPLACE);
                    }
                    
                    return true;
                }
            }
            return false;
        }
    }

    public class ScriptCondition
    {
        internal string PossibleNewValue;
        private const string COUNT = "COUNT";
        private const string BEGINNING = "BEGINNING";
        private string m_FullConditionUnparsed;
        private Dictionary<string, string> m_ConditionDictionary = new Dictionary<string, string>();

        public ScriptCondition(string condition, string newValue)
        {
            PossibleNewValue = newValue;
            m_FullConditionUnparsed = condition;
            CreateConditionDictionary();
        }

        private void CreateConditionDictionary()
        {
            string[] conditionSplit = m_FullConditionUnparsed.Split('&');
            foreach(string s in conditionSplit)
            {
                if (s.Trim() != string.Empty)
                {
                    string[] condition_Value = s.Split('=');
                    if (condition_Value.Length < 2) { throw new Exception($"Problem with script format, please see Condition: \n{m_FullConditionUnparsed}"); }
                    m_ConditionDictionary.Add(condition_Value[0].ToUpper(), condition_Value[1]);
                }
            }
        }

        internal bool TestCondition(string oldPiece)
        {
            bool passes = true;
            foreach (string key in m_ConditionDictionary.Keys)
            {
                switch (key.ToUpper())
                {
                    case COUNT:
                        passes = DoCountFunction(oldPiece);
                        break;
                    case BEGINNING:
                        passes = DoBeginningFunction(oldPiece);
                        break;
                    default:
                        passes = true;
                        break;
                }
                if (passes == false) { return false; }
            }
            return passes;
        }

        private bool DoBeginningFunction(string oldPiece)
        {
            string beginningVal = m_ConditionDictionary[BEGINNING];
            int bCount = beginningVal.Length;

            if(oldPiece.Substring(0, bCount) == beginningVal)
            {
                return true;
            }
            else { return false; }
        }

        private bool DoCountFunction(string oldPiece)
        {
            int countLength = int.Parse(m_ConditionDictionary[COUNT]);
            if(oldPiece.Replace("-","").Length == countLength)
            {
                return true;
            }
            else { return false; }
        }
    }

    public class ScriptAddtionalParameters
    {
        private Dictionary<string, string> m_Dictionary;

        public ScriptAddtionalParameters(Dictionary<string, string> d)
        {
            m_Dictionary = d;
        }

        public string Value(string key)
        {
            return m_Dictionary[key];
        }

        public bool ContainsK(string key)
        {
            return m_Dictionary.ContainsKey(key);
        }
    }
}