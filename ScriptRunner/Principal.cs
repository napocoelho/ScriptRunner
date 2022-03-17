using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

//using ScriptRunning.DataGridView;
using Connections;

namespace ScriptRunning
{
    public partial class Principal : Form
    {
        public Connections.ConnectionManager Manager { get; set; }
        public ScriptRunner Runner { get; set; }

        public Stopwatch StopWatch { get; set; }

        //DataGridViewDecorator GridDecorator { get; set; }
        private ScriptRunnerCallback Callback { get; set; }

        private object LOCK = new object();
        private ProgressStatusEnum lastStatus;
        private ProgressStatusEnum LastStatus { get { lock (LOCK) { return this.lastStatus; } } set { lock (LOCK) { this.lastStatus = value; } } }

        //private string FILE_INI = Application.CommonAppDataPath + @"\ScriptRunner.ini";
        private string FILE_INI = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\ScriptRunner.ini";


        private Dictionary<string, string> Parameters { get; set; }

        public Principal()
        {
            InitializeComponent();
            this.StopWatch = new Stopwatch();
            this.Parameters = new Dictionary<string, string>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.SetGridView();

            this.LastStatus = ProgressStatusEnum.None;
            this.btnExecutar.Enabled = true;
            this.btnPause.Enabled = false;
            this.btnStop.Enabled = false;

            folderBrowserDialog1.Description = "Select the script folder:";


            this.LoadIniParameters();

            txtFolder.Text = System.IO.Path.GetDirectoryName(Application.ExecutablePath);

            /*
            if( this.Parameters.ContainsKey ("PATH") )
            {
                if (System.IO.Directory.Exists(this.Parameters["PATH"]))
                {
                    txtFolder.Text = System.IO.Path.GetDirectoryName(this.Parameters["PATH"]);
                }
            }
            */

            folderBrowserDialog1.SelectedPath = txtFolder.Text;

            if (this.Parameters.ContainsKey("SERVER"))
            {
                txtServer.Text = this.Parameters["SERVER"];
            }

            if (this.Parameters.ContainsKey("TIMEOUT"))
            {
                txtTimeout.Text = this.Parameters["TIMEOUT"];
            }

            timer1.Start();

            cbxDatabases.Enabled = false;

            Application.DoEvents();
        }

        private void LoadIniParameters()
        {
            Dictionary<string, string> parameters = this.Parameters;

            parameters.Clear();

            try
            {
                if (System.IO.File.Exists(FILE_INI))
                {
                    foreach (string item in System.IO.File.ReadAllLines(FILE_INI, Encoding.Default))
                    {
                        string line = item;

                        /*
                        if (Regex.IsMatch(line, @"\[PATH\]=[.]*", RegexOptions.IgnoreCase))
                        {
                            line = line.Replace("[", "").Replace("]", "");
                            string[] split = Regex.Split(line, "=");
                            parameters.Add(split[0], split[1]);
                        }
                        else */

                        if (Regex.IsMatch(line, @"\[SERVER\]=[.]*", RegexOptions.IgnoreCase))
                        {
                            line = line.Replace("[", "").Replace("]", "");
                            string[] split = Regex.Split(line, "=");
                            parameters.Add(split[0], split[1]);
                        }
                        else if (Regex.IsMatch(line, @"\[TIMEOUT\]=[.]*", RegexOptions.IgnoreCase))
                        {
                            line = line.Replace("[", "").Replace("]", "");
                            string[] split = Regex.Split(line, "=");
                            parameters.Add(split[0], split[1]);
                        }
                        else if (Regex.IsMatch(line, @"\[DATABASE\]=[.]*", RegexOptions.IgnoreCase))
                        {
                            line = line.Replace("[", "").Replace("]", "");
                            string[] split = Regex.Split(line, "=");
                            parameters.Add(split[0], split[1]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Default parameters:
            if (!parameters.ContainsKey("PATH"))
            {
                parameters.Add("PATH", System.IO.Path.GetDirectoryName(Application.ExecutablePath));
            }

            if (!parameters.ContainsKey("SERVER"))
            {
                parameters.Add("SERVER", "SERVIDOR");
            }

            if (!parameters.ContainsKey("TIMEOUT"))
            {
                parameters.Add("TIMEOUT", "0");
            }

            if (!parameters.ContainsKey("DATABASE"))
            {
                parameters.Add("DATABASE", "master");
            }

            //return parameters;
        }

        private void SaveIniParameters()
        {
            List<string> lines = new List<string>();



            //this.Parameters["PATH"] = txtFolder.Text.Trim();
            this.Parameters["SERVER"] = txtServer.Text.Trim();

            this.Parameters["TIMEOUT"] = txtTimeout.Text.Trim();

            this.Parameters.Remove("DATABASE");

            if (!string.IsNullOrEmpty(cbxDatabases.Text))
            {
                this.Parameters["DATABASE"] = cbxDatabases.Text.Trim();
            }

            try
            {
                foreach (KeyValuePair<string, string> pair in this.Parameters)
                {
                    lines.Add(string.Format("[{0}]={1}", pair.Key, pair.Value));
                }

                System.IO.File.WriteAllLines(FILE_INI, lines.ToArray(), Encoding.Default);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetGridView()
        {
            // Limpando:
            this.Grid.Rows.Clear();
            this.Grid.Columns.Clear();
            this.Grid.DataSource = null;
            this.Grid.RowHeadersVisible = false;

            // Preenchendo:
            this.Grid.AutoSize = false;
            this.Grid.AllowUserToAddRows = false;   // *** se essa bosta tiver TRUE, uma linha branca sempre ficará ativa no final do grid ***
            this.Grid.AutoGenerateColumns = false;  // *** se não desativar esta propriedade, o controle não deixará criar manualmente ***
            //this.Grid.DataSource = dataSource;
            this.Grid.AllowUserToResizeRows = false;
            this.Grid.AllowUserToResizeColumns = false;
            this.Grid.RowHeadersVisible = false;

            this.Grid.Columns.Add("Property", "Property");
            this.Grid.Columns.Add("Value", "Value");
            this.Grid.Columns["Value"].AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            //this.Grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);


            // Opcional:
            //this.Grid.ReadOnly = true;
            this.Grid.Update();
            this.Grid.Refresh();
        }

        private void UpdateGridView(ViewStatus view)
        {
            this.Grid.Rows.Clear();
            this.Grid.Rows.Add("Loading", view.Loading + "%");
            this.Grid.Rows.Add("Executing", view.Executing + "%");
            this.Grid.Rows.Add("DetailedExecuting", view.DetailedExecuting + "%");
            this.Grid.Rows.Add("FileName", view.FileName);
            this.Grid.Rows.Add("Path", view.Path);
            this.Grid.Rows.Add("Progress", view.Progress);
            this.Grid.Rows.Add("Error", view.Error);
            this.Grid.Rows.Add("Command", view.Command);

            this.Grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);


            // Opcional:
            this.Grid.ReadOnly = true;
            this.Grid.Update();
            this.Grid.Refresh();
        }

        public ConnectionManager Connect(string server, string database)
        {
            List<string> initialCommands = new List<string>();
            string connectionString = "";

            initialCommands.Add("SET LANGUAGE 'Português (Brasil)'");
            initialCommands.Add("SET LOCK_TIMEOUT -1");

            server = server.Trim();
            database = database.Trim();

            connectionString = "Initial Catalog=" + database + ";" +
                                "Data Source=" + server + ";" +
                                "User ID=sistema;" +
                                "Password=schwer_wissen;" +
                                "Connect Timeout=0;" +
                                "Application Name='Script Runner'";

            return ConnectionManager.CreateInstance(connectionString);
        }



        void Runner_StatusChanged(object sender, ScriptRunnerStatusEventArgs e)
        {
            GuiTaskSync.Run(() =>
            {
                ViewStatus view = new ViewStatus();

                view.Loading = decimal.Round(e.LoadProgression, 2);
                view.Executing = decimal.Round(e.ExecutingProgression, 2);
                view.DetailedExecuting = decimal.Round(e.CommandProgression, 2);
                view.FileName = e.ExecutingFileName;
                view.Path = e.ExecutingPath;
                view.Progress = e.Progress;

                if (e.FailedException != null)
                {
                    view.Command = e.ExecutingCommand.Content;
                }
                else
                {
                    view.Command = string.Empty;
                }

                if (e.FailedException != null)
                {
                    view.Error = e.FailedException.Message;
                }
                else
                {
                    view.Error = string.Empty;
                }

                this.UpdateGridView(view);

                Application.DoEvents();
            });
        }

        private void btnExecutar_Click(object sender, EventArgs e)
        {
            try
            {
                btnExecutar.Enabled = false;

                cbxDatabases.Enabled = false;
                btnConnect.Enabled = false;
                txtServer.Enabled = false;
                txtTimeout.Enabled = false;

                this.StopWatch.Reset();

                try
                {
                    this.SaveIniParameters();
                }
                catch (Exception ex)
                { }

                this.LastStatus = ProgressStatusEnum.None;

                this.Manager.Connection.Connection.ChangeDatabase(this.Parameters["DATABASE"]);

                //string fileName = @"F:\GestPlus_Docs\Scripts\Opera\Atualizacao_2015_11_04_B_MDFe.sql";
                string dirName = @"C:\Users\Administrador\Desktop\Operação Varejo no Limbo\Passo 3 (atualizações do opera)\2015";
                dirName = @"C:\Users\Administrador\Desktop\Operação Varejo no Limbo";

                dirName = txtFolder.Text;

                string[] sqlFiles = System.IO.Directory.GetFiles(dirName, "*.sql").OrderBy(x => x).ToArray();



                this.Runner = new ScriptRunner(this.Manager, this.Parameters["DATABASE"]);
                this.Runner.StatusChanged += Runner_StatusChanged;

                this.Callback = this.Runner.ExecuteScriptsAsync(sqlFiles);

                /*
                this.Callback.Changed += (senderX, eX) =>
                    {
                        try
                        {
                            this.LastStatus = this.Callback.Status;
                        }
                        catch (Exception ex)
                        {
                            string msg = ex.Message;
                        }
                    };
                */

                this.Callback.Execute();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                btnExecutar.Enabled = true;
                btnPause.Enabled = false;
                btnStop.Enabled = false;

                MessageBox.Show(ex.Message);
            }
            finally
            {

            }
        }


        private void btnPause_Click(object sender, EventArgs e)
        {
            if (this.Callback.Status == ProgressStatusEnum.Paused)
            {
                this.Callback.Continue();
            }
            else
            {
                this.Callback.Pause();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.Callback.Stop();
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = "Choose a folder";
            DialogResult result = folderBrowserDialog1.ShowDialog(this);

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.txtFolder.Text = folderBrowserDialog1.SelectedPath;

                try
                {
                    System.IO.File.WriteAllText(FILE_INI, folderBrowserDialog1.SelectedPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan time = this.StopWatch.Elapsed;
            //lblElapsedTime.Text =      string.Format("{0}:{1}:{2}",       time.to
            lblElapsedTime.Text = "Elapsed time: " + time.ToString();



            if (this.Callback != null && this.LastStatus != this.Callback.Status)
            {
                this.LastStatus = this.Callback.Status;

                if (this.LastStatus == ProgressStatusEnum.Concluded)
                {
                    this.StopWatch.Stop();

                    btnExecutar.Enabled = true;
                    btnPause.Enabled = false;
                    btnStop.Enabled = false;

                    cbxDatabases.Enabled = true;
                    btnConnect.Enabled = true;
                    txtServer.Enabled = true;
                    txtTimeout.Enabled = true;

                    FlashWindow.Start(this);
                }
                else if (this.LastStatus == ProgressStatusEnum.Stopped)
                {
                    this.StopWatch.Stop();

                    btnExecutar.Enabled = true;
                    btnPause.Enabled = false;
                    btnStop.Enabled = false;

                    cbxDatabases.Enabled = true;
                    btnConnect.Enabled = true;
                    txtServer.Enabled = true;
                    txtTimeout.Enabled = true;
                }
                else if (this.LastStatus == ProgressStatusEnum.Paused)
                {
                    this.StopWatch.Stop();

                    btnExecutar.Enabled = false;
                    btnPause.Enabled = true;
                    btnStop.Enabled = false;

                    cbxDatabases.Enabled = false;
                    btnConnect.Enabled = false;
                    txtServer.Enabled = false;
                    txtTimeout.Enabled = false;
                }
                else if (this.LastStatus == ProgressStatusEnum.Failed)
                {
                    this.StopWatch.Stop();

                    btnExecutar.Enabled = true;
                    btnPause.Enabled = false;
                    btnStop.Enabled = false;

                    cbxDatabases.Enabled = true;
                    btnConnect.Enabled = true;
                    txtServer.Enabled = true;
                    txtTimeout.Enabled = true;

                    FlashWindow.Start(this);
                }
                else if (this.LastStatus == ProgressStatusEnum.Running)
                {
                    btnExecutar.Enabled = false;
                    btnPause.Enabled = true;
                    btnStop.Enabled = true;

                    cbxDatabases.Enabled = false;
                    btnConnect.Enabled = false;
                    txtServer.Enabled = false;
                    txtTimeout.Enabled = false;

                    this.StopWatch.Start();
                }

                Application.DoEvents();
            }
        }

        private void Principal_Activated(object sender, EventArgs e)
        {
            FlashWindow.Stop(this);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                grpControls.Enabled = false;

                this.Manager = this.Connect(txtServer.Text.Trim(), "master");
                this.Manager.Connection.ExecuteNonQuery("SET LOCK_TIMEOUT " + txtTimeout.Text);


                System.Data.DataTable tblDatabases = this.Manager.Connection.ExecuteDataTable("select name from sys.databases");
                List<string> dataSource = new List<string>();

                foreach (System.Data.DataRow row in tblDatabases.Rows)
                {
                    dataSource.Add(row["name"].ToString().Trim());
                }

                cbxDatabases.DataSource = null;
                //cbxDatabases.DisplayMember = "name";
                //cbxDatabases.ValueMember = "name";
                cbxDatabases.DataSource = dataSource;
                cbxDatabases.Enabled = true;

                if (this.Parameters.ContainsKey("DATABASE"))
                {
                    foreach (string item in dataSource)
                    {
                        if (item.Equals(this.Parameters["DATABASE"], StringComparison.OrdinalIgnoreCase))
                        {
                            cbxDatabases.SelectedItem = this.Parameters["DATABASE"];
                            cbxDatabases.Text = this.Parameters["DATABASE"];
                            break;
                        }
                    }
                }

                this.SaveIniParameters();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                grpControls.Enabled = true;
            }
        }

        private void cbxDatabases_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cbxDatabases.SelectedIndex == -1 || string.IsNullOrEmpty(cbxDatabases.Text))
                {
                    btnExecutar.Enabled = false;
                }
                else
                {
                    this.Manager.Connection.ExecuteNonQuery("use " + cbxDatabases.Text);
                    btnExecutar.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cbxDatabases_TextUpdate(object sender, EventArgs e)
        {
            try
            {
                List<string> itens = this.cbxDatabases.DataSource as List<string>;

                btnExecutar.Enabled = false;

                for (int idx = 0; idx < itens.Count; idx++)
                {
                    if (itens[idx].Equals(cbxDatabases.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Manager.Connection.ExecuteNonQuery("use " + cbxDatabases.Text);
                        btnExecutar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                btnExecutar.Enabled = false;
                MessageBox.Show(ex.Message);
            }
        }

        private void txtTimeout_TextChanged(object sender, EventArgs e)
        {
            //txtTimeout.Text = Regex.Replace(txtTimeout.Text, @"[^\-]{1,}[\D]", "");
        }

        private void txtTimeout_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                txtTimeout.Text = int.Parse(txtTimeout.Text).ToString();
            }
            catch (Exception ex)
            {
                txtTimeout.Text = "0";
            }
        }

        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                this.SaveIniParameters();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private void Principal_Enter(object sender, EventArgs e)
        {
            FlashWindow.Stop(this);
        }
    }

    public class ViewStatus
    {
        public decimal Loading { get; set; }
        public decimal Executing { get; set; }
        public decimal DetailedExecuting { get; set; }

        public string FileName { get; set; }
        public string Path { get; set; }

        public string Command { get; set; }

        public string Error { get; set; }

        public ProgressStatusEnum Progress { get; set; }

        public ViewStatus()
        { }

    }
}