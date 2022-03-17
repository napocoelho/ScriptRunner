using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Connections;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace ScriptRunning
{
    /// <summary>
    /// 
    /// </summary>
    public class ScriptRunner
    {
        //private List<string> Files { get; set; }
        //private List<Script> Scripts { get; set; }
        //private List<Script> Scripts { get; set; }

        private ConnectionManager Manager { get; set; }
        public ScriptRunnerStatus Status { get; set; }
        
        private string DefaultDatabase { get; set; }

        private ScriptRunnerCallback Callback { get; set; }

        public event EventHandler<ScriptRunnerStatusEventArgs> StatusChanged;
        private void OnStatusChanged()
        {
            if (this.StatusChanged != null)
            {
                this.StatusChanged(this, new ScriptRunnerStatusEventArgs(this.Status));
            }
        }

        public ScriptRunner(Connections.ConnectionManager manager, string defaultDatabase)
        {
            this.Status = new ScriptRunnerStatus();
            this.Manager = manager;
            this.DefaultDatabase = defaultDatabase;
        }

        public ScriptRunnerCallback ExecuteScriptsAsync(string[] filePaths)
        {
            this.DefaultDatabase = this.Manager.Connection.Connection.Database;

            Action executeAction = () =>
            {
                this.Callback.Status = ProgressStatusEnum.Running;

                Task.Run(() =>
                {
                    try
                    {
                        ExecuteScripts(filePaths);
                    }
                    catch (Exception ex)
                    {
                        this.Callback.Exception = ex;
                        this.Callback.Status = ProgressStatusEnum.Failed;
                    }
                    finally
                    {

                    }
                });
            };

            this.Callback = new ScriptRunnerCallback(executeAction);

            return this.Callback;
        }

        private void ExecuteScripts(string[] filePaths)
        {
            try
            {
                this.Status.Progress = ProgressStatusEnum.Running;
                this.Status.ExecutingProgression = 0m;
                this.OnStatusChanged();

                this.Manager.Connection.Connection.ChangeDatabase(this.DefaultDatabase);

                try
                {
                    if (System.IO.File.Exists(@"log.txt"))
                        System.IO.File.Delete(@"log.txt");
                }
                catch (Exception ex)
                { }

                ConcurrentQueue<string> queue = new ConcurrentQueue<string>(filePaths);

                SynchronizedCollection<Script> loadedScripts = this.AsyncLoadScripts(queue, 30);


                for (int idx = 0; idx < filePaths.Count(); idx++)
                {
                    string file = filePaths[idx];

                    this.Status.ExecutingFileName = System.IO.Path.GetFileName(file);
                    this.OnStatusChanged();

                    // Comparação padrão:
                    Predicate<Script> foundScript = item => item.Path.Equals(file, StringComparison.OrdinalIgnoreCase);

                    while (!loadedScripts.Contains(foundScript))
                    {
                        if (this.Callback.Status == ProgressStatusEnum.Failed)
                        {
                            throw this.Callback.Exception;
                        }

                        System.Threading.Thread.Sleep(100);
                    }

                    Script script = null;

                    while (this.Callback.Status == ProgressStatusEnum.Paused)
                    {
                        if (this.Callback.Status == ProgressStatusEnum.Failed)
                        {
                            throw this.Callback.Exception;
                        }

                        System.Threading.Thread.Sleep(100);
                    }

                    if (!loadedScripts.Remove(foundScript, out  script))
                    {
                        throw new Exception("Script loading fail!!");
                    }


                    this.Status.ExecutingScript = script;
                    this.OnStatusChanged();

                    if (script.Commands.Where(command => command.Status == CommandStatusEnum.Waiting).Count() > 0)
                    {
                        this.Execute(script);
                    }

                    this.Status.ExecutingProgression = 100m / filePaths.Count() * (idx + 1);
                    this.OnStatusChanged();
                }

                this.Status.ExecutingProgression = 100m;
                this.Status.Progress = ProgressStatusEnum.Concluded;
                this.Callback.Status = ProgressStatusEnum.Concluded;
                this.OnStatusChanged();
            }
            catch (Exception ex)
            {
                this.Callback.Status = ProgressStatusEnum.Failed;
                this.Callback.Exception = ex;
                this.Status.FailedException = ex;
                this.Status.Progress = ProgressStatusEnum.Failed;
            }
            finally
            {
                this.OnStatusChanged();
            }
        }


        private SynchronizedCollection<Script> AsyncLoadScripts(ConcurrentQueue<string> queue, int maxQueueSize = 20)
        {


            //ScriptRunnerCallback<SynchronizedCollection<Script>> callback = new ScriptRunnerCallback<SynchronizedCollection<Script>>();
            SynchronizedCollection<Script> loadedScripts = new SynchronizedCollection<Script>();

            //ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            int totalFiles = queue.Count;


            Task.Run(() =>
            {
                int idx = 0;

                try
                {
                    this.Status.LoadProgression = 0m;
                    this.OnStatusChanged();

                    while (!queue.IsEmpty)
                    {
                        if (this.Callback.Status == ProgressStatusEnum.Failed || this.Callback.Status == ProgressStatusEnum.Stopped || this.Callback.Status == ProgressStatusEnum.Concluded)
                            break;

                        idx++;
                        string path = null;
                        queue.TryDequeue(out path);


                        while (loadedScripts.Count > maxQueueSize)
                        {
                            System.Threading.Thread.Sleep(30);

                            if (this.Callback.Status == ProgressStatusEnum.Failed || this.Callback.Status == ProgressStatusEnum.Stopped)
                                break;
                        }

                        if (this.Callback.Status == ProgressStatusEnum.Failed || this.Callback.Status == ProgressStatusEnum.Stopped)
                            return;

                        while (this.Callback.Status == ProgressStatusEnum.Paused)
                        {
                            System.Threading.Thread.Sleep(200);
                        }

                        if (this.Callback.Status == ProgressStatusEnum.Stopped)
                            break;

                        Script script = Script.LoadFromFile(path);
                        loadedScripts.Add(script);

                        this.Status.LoadProgression = 100m / totalFiles * (idx + 1);
                        this.OnStatusChanged();
                    }

                    if (this.Callback.Status == ProgressStatusEnum.Failed)
                        return;

                    //this.Callback.Status = ProgressStatusEnum.Concluded;

                    this.Status.LoadProgression = 100m;
                    this.OnStatusChanged();
                }
                catch (Exception ex)
                {
                    this.Callback.Status = ProgressStatusEnum.Failed;
                    this.Callback.Exception = ex;
                }
                finally
                {
                    this.OnStatusChanged();
                }
            });


            return loadedScripts;
        }

        private void Execute(Script script)
        {
            this.Status.ExecutingScript = script;
            this.OnStatusChanged();

            int commandIndex = 0;

            this.Status.CommandProgression = 0m;
            this.OnStatusChanged();

            // Voltando o DATABASE para o valor inicial:
            this.Manager.Connection.Connection.ChangeDatabase(this.DefaultDatabase);
            this.Manager.Connection.ExecuteNonQuery("use " + this.DefaultDatabase);

            foreach (Command command in script.Commands)
            {
                commandIndex++;

                this.Status.ExecutingCommand = command;
                this.Status.CommandProgression = 100m / script.Commands.Count * commandIndex;
                this.OnStatusChanged();

                if (command.Status == CommandStatusEnum.Waiting)
                {
                    Stopwatch stopwatch = new Stopwatch();

                    try
                    {
                        command.ExecutionTime = TimeSpan.FromMilliseconds(0);
                        command.Exception = null;
                        command.Status = CommandStatusEnum.Executing;

                        stopwatch.Start();
                        try
                        {
                            this.Manager.Connection.ExecuteNonQuery(command.Content);
                        }
                        catch(Exception ex)
                        {
                            if(command.IgnoreError )
                            {
                                StringBuilder builder  = new StringBuilder("");
                                if(System.IO.File.Exists(@"log.txt"))
                                    builder.Append( System.IO.File.ReadAllText (@"log.txt", Encoding.Default ));

                                builder.AppendLine("------------------------------------------------------------------------------------");
                                builder.AppendLine(DateTime.Now.ToString());
                                builder.AppendLine("Script: " + script.FileName );
                                builder.AppendLine("Command: " + command.Content  );
                                builder.AppendLine("Exception: " + ex.Message   );
                                System.IO.File.WriteAllText(@"log.txt", builder.ToString(), Encoding.Default);
                                builder.AppendLine();
                                builder.AppendLine();
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                        stopwatch.Stop();

                        command.Status = CommandStatusEnum.Successeful;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        
                        command.Status = CommandStatusEnum.Failed;
                        command.Exception = ex;
                        string msg = ex.Message;

                        throw ex;
                    }
                    finally
                    {
                        command.ExecutionTime = stopwatch.Elapsed;
                    }
                }

                while (this.Callback.Status == ProgressStatusEnum.Paused)
                {
                    System.Threading.Thread.Sleep(200);
                }

                if (this.Callback.Status == ProgressStatusEnum.Stopped)
                    return;
            }

            // Voltando o DATABASE para o valor inicial:
            this.Manager.Connection.ExecuteNonQuery("USE " + this.DefaultDatabase);

            this.Status.CommandProgression = 100m;
            this.OnStatusChanged();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class ScriptRunnerStatus
    {
        private object LOCK = new object();

        private ProgressStatusEnum progress;
        public ProgressStatusEnum Progress { get { return this.progress; } set { lock (LOCK) { this.progress = value; } } }

        private decimal loadProgression;
        public decimal LoadProgression { get { return this.loadProgression; } set { lock (LOCK) { this.loadProgression = value; } } }

        private decimal executingProgression;
        public decimal ExecutingProgression { get { return this.executingProgression; } set { lock (LOCK) { this.executingProgression = value; } } }

        private decimal commandProgression;
        public decimal CommandProgression { get { return this.commandProgression; } set { lock (LOCK) { this.commandProgression = value; } } }

        private string executingFileName;
        public string ExecutingFileName { get { return this.executingFileName; } set { lock (LOCK) { this.executingFileName = value; } } }

        private string executingPath;
        public string ExecutingPath { get { return this.executingPath; } set { lock (LOCK) { this.executingPath = value; } } }

        private Script executingScript;
        public Script ExecutingScript { get { return this.executingScript; } set { lock (LOCK) { this.executingScript = value; } } }

        private Command executingCommand;
        public Command ExecutingCommand { get { return this.executingCommand; } set { lock (LOCK) { this.executingCommand = value; } } }

        private Exception failedException;
        public Exception FailedException { get { return this.failedException; } set { lock (LOCK) { this.failedException = value; } } }

        //private int commandIndexFailed;
        //public int CommandIndexFailed { get { return this.commandIndexFailed; } set { lock (LOCK) { this.commandIndexFailed = value; } } }

        public ScriptRunnerStatus()
        {
            this.Progress = ProgressStatusEnum.None;

            this.LoadProgression = 0m;
            this.ExecutingProgression = 0m;
            this.CommandProgression = 0m;

            this.ExecutingFileName = null;
            this.ExecutingScript = null;
            this.ExecutingCommand = null;
            this.FailedException = null;
            //this.CommandIndexFailed = -1;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ScriptRunnerStatusEventArgs : EventArgs
    {
        public ProgressStatusEnum Progress { get; set; }

        public decimal LoadProgression { get; set; }
        public decimal ExecutingProgression { get; set; }
        public decimal CommandProgression { get; set; }

        public string ExecutingFileName { get; set; }
        public string ExecutingPath { get; set; }

        public Command ExecutingCommand { get; set; }
        public Exception FailedException { get; set; }

        public ScriptRunnerStatusEventArgs(ScriptRunnerStatus status)
        {
            this.Progress = status.Progress;

            this.LoadProgression = status.LoadProgression;
            this.ExecutingProgression = status.ExecutingProgression;
            this.CommandProgression = status.CommandProgression;

            this.ExecutingFileName = status.ExecutingFileName;
            this.ExecutingPath = status.ExecutingPath;

            this.ExecutingCommand = status.ExecutingCommand;
            this.FailedException = status.FailedException;
        }
    }

    public enum ProgressStatusEnum
    {
        None, Concluded, Paused, Stopped, Running, Failed
    }

    public class ScriptRunnerCallback
    {
        protected object LOCK_STATUS = new object();
        protected object LOCK_EXCEPTION = new object();

        private ProgressStatusEnum status;
        public ProgressStatusEnum Status
        {
            get { lock (LOCK_STATUS) { return status; } }
            set
            {
                bool changed = false;

                lock (LOCK_STATUS)
                {
                    if (this.status != value)
                    {
                        this.status = value;
                        changed = true;
                    }
                }

                if (changed)
                    OnChanged();
            }
        }

        private Exception exception;
        public Exception Exception
        {
            get { lock (LOCK_EXCEPTION) { return exception; } }
            set
            {
                lock (LOCK_EXCEPTION)
                {
                    bool changed = false;

                    if (this.exception != value)
                    {
                        this.exception = value;
                        changed = true;
                    }

                    if (changed)
                        OnChanged();
                }
            }
        }

        public event EventHandler Changed;
        private void OnChanged()
        {
            if (this.Changed != null)
            {
                this.Changed(this, new EventArgs());
            }
        }

        private Action ExecuteScriptRunnerAction { get; set; }

        public ScriptRunnerCallback(Action executeScriptRunnerAction)
        {
            this.ExecuteScriptRunnerAction = executeScriptRunnerAction;
            this.Status = ProgressStatusEnum.None;
            this.Exception = null;
        }

        public void Execute()
        {
            this.Status = ProgressStatusEnum.Running;
            this.ExecuteScriptRunnerAction();
        }

        public void Pause()
        {
            this.Status = ProgressStatusEnum.Paused;
        }

        public void Continue()
        {
            this.Status = ProgressStatusEnum.Running;
        }

        public void Stop()
        {
            this.Status = ProgressStatusEnum.Stopped;
        }
    }
}