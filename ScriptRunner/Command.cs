using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScriptRunning
{
    public class Command
    {
        public string Content { get; set; }
        //public int StartingLine { get; set; }
        //public int LinesCount { get; set; }

        public TimeSpan ExecutionTime { get; set; }

        public CommandStatusEnum Status { get; set; }
        
        public Exception Exception { get; set; }

        public bool IgnoreError { get; set; }

        public Command()
        {
            this.Content = string.Empty;
            //this.StartingLine = 0;
            //this.LinesCount = 0;            
            this.Status = CommandStatusEnum.Waiting;
            this.Exception = null;
            this.ExecutionTime = TimeSpan.FromMilliseconds(0);

            this.IgnoreError = false;
        }

        public Command(string content) //, int startingLine)
        {
            string[] SPLITTER = new string[] { Environment.NewLine };

            this.Content = content;
            //this.StartingLine = startingLine;
            //this.LinesCount = content.Split(SPLITTER, StringSplitOptions.None).Count();
        }
    }

    public enum CommandStatusEnum
    {
        Waiting, Executing, Successeful, Failed
    }
}