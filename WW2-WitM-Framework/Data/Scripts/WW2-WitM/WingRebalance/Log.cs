using System;
using Sandbox.ModAPI;
using VRage.Utils;

namespace WW2WitM.Aerodynamics
{
    // Minimal logger, only writes to the SE log console - no file/notification plumbing needed for this mod.
    public static class Log
    {
        public static string modName = "WW2-WitM Wing Rebalance";

        public static void Error(Exception e)
        {
            MyLog.Default.WriteLineAndConsole(modName + " error: " + e);
        }

        public static void Error(Exception e, string context)
        {
            MyLog.Default.WriteLineAndConsole(modName + " error (" + context + "): " + e);
        }

        public static void Info(string msg)
        {
            MyLog.Default.WriteLineAndConsole(modName + ": " + msg);
        }
    }
}
