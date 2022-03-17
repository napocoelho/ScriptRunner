using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace ScriptRunning
{
    public class Script
    {
        public string FileName { get; set; }
        public string Path { get; set; }
        //public string Text { get; set; }
        public List<Command> Commands { get; private set; }

        public Script()
        {
            this.Path = string.Empty;
            //this.Text = string.Empty;
            this.Commands = new List<Command>();
        }



        public static Script LoadFromFile(string path)
        {
            string text = null;
            //string text = System.IO.File.ReadAllText(path, Encoding.Default);
            Script script = new Script();
            script.Path = path;
            script.FileName = System.IO.Path.GetFileName(script.Path);
            //script.Text = text;
            //int startingLineCount = 0;

            string[] lines = System.IO.File.ReadAllLines(path, Encoding.Default);






            //-------------------------------------------- Finding MACROs:

            string pattern = MacroPatterns.IgnoreErrorOn;
            List<MacroBlock> blocks = new List<MacroBlock>();

            blocks.Add(new MacroBlock());  // precisa ter pelo menos 1 bloco;

            for (int idx = 0; idx < lines.Count(); idx++)
            {
                if (Regex.IsMatch(lines[idx], MacroPatterns.IgnoreErrorOn) && blocks.Last().IgnoreError == false)
                {
                    blocks.Add(new MacroBlock());
                    blocks.Last().IgnoreError = true;
                }
                else if (Regex.IsMatch(lines[idx], MacroPatterns.IgnoreErrorOff) && blocks.Last().IgnoreError == true)
                {
                    blocks.Add(new MacroBlock());
                    blocks.Last().IgnoreError = false;
                }

                blocks.Last().Lines.Add(lines[idx]);
            }


            //-------------------------------------------- Adding Commands to Script:
            foreach (MacroBlock block in blocks)
            {
                List<Command> commands = ParseLines(block.Lines);

                foreach( Command command in commands )
                {
                    command.IgnoreError = block.IgnoreError;
                    script.Commands.Add(command);
                }
            }



            /*
            foreach (string commandText in split)
            {
                startingLineCount = startingLineCount == 0 ? 1 : startingLineCount;

                if (string.IsNullOrEmpty(commandText.Trim()) || commandText.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    startingLineCount += 1;
                }
                else
                {
                    Command command = new Command(commandText, startingLineCount);
                    script.Commands.Add(command);
                    startingLineCount = command.StartingLine + command.LinesCount;
                }
            }
            */

            return script;
        }

        public static List<Command> ParseLines(List<string> lines)
        {
            List<Command> commands = new List<Command>();
            StringBuilder builder = new StringBuilder("");
            

            //-------------------------------------------- Removing comments:
            string line = null;

            // Remove comentários de linha (--);
            for (int idx = 0; idx < lines.Count(); idx++)
            {
                line = Regex.Replace(lines[idx], @"(?<=^([^'""]|['][^']*[']|[""][^""]*[""])*)(--.*$|/\*(.|\n)*?\*/)", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                builder.AppendLine(line);
            }

            //text = Regex.Replace(text, @"(?<=^([^'""]|['][^']*[']|[""][^""]*[""])*)(--.*$|/\*(.|\n)*?\*/)", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

            // Remove comentários de bloco (/**/);
            string text = Regex.Replace(builder.ToString(), @"/\*(?>(?:(?>[^*]+)|\*(?!/))*)\*/", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            
            //--------------------------------------------

            //string[] split = Regex.Split(text, @"^\s*GO\W*\s*(?<!/\*[^/\*]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled); // considera GO dentro de comentários (mais completo e lento)
            string[] split = Regex.Split(text, @"^[ \t]*GO\W*[ \t]*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);  // desconsidera GO dentro de comentários (mais rápido, mas não funciona em todos os tipos de situações)


            foreach (string commandText in split)
            {
                //startingLineCount = startingLineCount == 0 ? 1 : startingLineCount;

                if (string.IsNullOrEmpty(commandText.Trim()) || commandText.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    //startingLineCount += 1;
                }
                else
                {
                    Command command = new Command(commandText);
                    commands.Add(command);
                    //startingLineCount = command.StartingLine + command.LinesCount;
                }
            }

            return commands;
        }

        public override int GetHashCode()
        {
            return this.FileName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            Script another = obj as Script;

            if (another == null)
                return false;

            if (this.FileName.Equals(another.FileName))
                return true;

            return false;
        }
    }

    public class MacroBlock
    {
        public List<string> Lines { get; set; }
        public bool IgnoreError { get; set; }

        public MacroBlock()
        {
            this.Lines = new List<string>();
            this.IgnoreError = false;
        }
    }
}