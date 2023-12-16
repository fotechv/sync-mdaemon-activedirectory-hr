using System.Collections.Generic;
using System.Diagnostics;
using Hpl.Common.MdaemonServices;

namespace Hpl.Common
{
    public class ServerStatus
    {
        public static bool GetProcessAcm()
        {
            Process[] processCollection = Process.GetProcesses();
            foreach (Process p in processCollection)
            {
                if (p.ProcessName == "Hpl.Acm")
                {
                    return true;
                }
            }

            return false;
        }

        public static List<string> ListProcesses()
        {
            var list = new List<string>();
            Process[] processCollection = Process.GetProcesses();
            foreach (Process p in processCollection)
            {
                list.Add(p.ProcessName);
            }

            return list;
        }
    }
}