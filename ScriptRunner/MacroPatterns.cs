using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptRunning
{
    public static class MacroPatterns
    {
        private static string MakeRegexMacro(string command, string modifier)
        {
            string pattern = @"(?<=^*?[ \t]*--[ \t]*)#[ \t]*{0}[ \t]*\=[ \t]*{1}[ \t]*#(?=[ \t]*$)";
            string result = string.Format(pattern, command, modifier);
            return result;
        }

        public static string IgnoreErrorOn
        {
            get
            {
                return MakeRegexMacro("IGNORE_ERROR", "ON");
            }
        }

        public static string IgnoreErrorOff
        {
            get
            {
                return MakeRegexMacro("IGNORE_ERROR", "OFF");
            }
        }
    }
}