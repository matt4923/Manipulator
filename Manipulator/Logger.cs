using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manipulator
{
    static class Logger
    {
        private const string LOG = "ManiLog.log";

        public static void WriteLog(string logStr)
        {
            if (!File.Exists(LOG))
            {
                using (StreamWriter writer = File.CreateText(LOG))
                {
                    DateTime dt = DateTime.Now;
                    writer.WriteLine($"{dt}\t{logStr}");
                }
            }
            else
            {
                using (StreamWriter writer = File.AppendText(LOG))
                {
                   DateTime dt = DateTime.Now;
                   writer.WriteLine($"{dt}\t{logStr}");
                }
            }
        }
    }
}
