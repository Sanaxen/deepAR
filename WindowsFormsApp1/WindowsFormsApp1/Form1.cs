using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApplication1;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        string InputFileName = "";
        public static string MyPath = "";   //exeが置いてあるパス
        string wrkdir = @"C:\Users\bccat\Desktop\deepARapp\WindowsFormsApp1\WindowsFormsApp1\bin";
        string Rdir = @"D:\Program Files\R\R-3.6.1\bin\x64";
        string python_venv = @"D:\Users\bccat\Anaconda3\envs\GluonTS";
        string PythonEnv = "";
        string Script_path = "";

        string forecast_plot = "";
        string rmse = "unknown";
        string MSE = "unknown";
        string MASE = "unknown";
        string MAPE = "unknown";
        string NRMSE = "unknown";

        public ImageView _ImageView;
        public bool train_mode = true;
        public Form1 form1 = null;

        public Form1()
        {
            InitializeComponent();

            form1 = this;
            MyPath = System.AppDomain.CurrentDomain.BaseDirectory;

            Script_path = MyPath + @"..\script\";
            wrkdir = MyPath + @"..\wrk";

            string backend = "";
            if (System.IO.File.Exists(MyPath + "..\\backend.txt"))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(MyPath + "..\\backend.txt", Encoding.GetEncoding("SHIFT_JIS"));
                if (sr != null)
                {
                    backend = sr.ReadToEnd().Replace("\n", "").Replace("\r", "");

                }
                if (sr != null) sr.Close();
            }
            if (backend != "")
            {
                Rdir = backend + @"\bin\x64";
            }

            if (System.IO.File.Exists(MyPath + "..\\python_venv.txt"))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(MyPath + "..\\python_venv.txt", Encoding.GetEncoding("SHIFT_JIS"));
                if (sr != null)
                {
                    string line = sr.ReadLine().Replace("\n", "").Replace("\r", "");
                    python_venv = line;

                    PythonEnv = python_venv + ";";
                    while (sr.EndOfStream == false)
                    {
                        line = sr.ReadLine().Replace("\n", "").Replace("\r", "");
                        PythonEnv += python_venv + line + ";";
                    }
                    PythonEnv += @"%PATH%";
                }
                if (sr != null) sr.Close();

            }
            
            string [] argv = Environment.GetCommandLineArgs();
            int argc = argv.Length;

            if ( argc >= 2 )
            {
                string dir = argv[1];
                try
                {
                    System.IO.Directory.SetCurrentDirectory(dir);
                    wrkdir = dir;
                }
                catch { }
            }
            if ( argc >= 3)
            {
                string file = InputFileName;
                InputFileName = argv[2];
                try
                {
                    ListBoxReset();
                    input_format();
                }
                catch { }
            }
       }

        void KillProcessTree(System.Diagnostics.Process process)
        {
            string taskkill = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe");
            using (var procKiller = new System.Diagnostics.Process())
            {
                procKiller.StartInfo.FileName = taskkill;
                procKiller.StartInfo.Arguments = string.Format("/PID {0} /T /F", process.Id);
                procKiller.StartInfo.CreateNoWindow = true;
                procKiller.StartInfo.UseShellExecute = false;
                procKiller.Start();
                procKiller.WaitForExit();
            }
        }

        public static System.Drawing.Image CreateImage(string filename)
        {
            System.IO.FileStream fs = new System.IO.FileStream(
                filename,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
            fs.Close();
            return img;
        }

        System.Diagnostics.ProcessStartInfo app_train = new System.Diagnostics.ProcessStartInfo();
        System.Diagnostics.Process process_train = null;

        System.Diagnostics.ProcessStartInfo app_test = new System.Diagnostics.ProcessStartInfo();
        System.Diagnostics.Process process_test = null;

        System.Diagnostics.ProcessStartInfo r_script = new System.Diagnostics.ProcessStartInfo();
        System.Diagnostics.Process process_r = null;



        void Rscript(string script, string args)
        {
            System.IO.Directory.SetCurrentDirectory(wrkdir);

            r_script.FileName = Rdir + @"\Rscript.exe";
            r_script.Arguments = " " + script;
            r_script.Arguments += args;
            r_script.UseShellExecute = false;

            process_r = System.Diagnostics.Process.Start(r_script);
            process_r.WaitForExit();
        }

        string deepar_common_code()
        {
            //
            using (System.IO.StreamReader sr1 = new System.IO.StreamReader(Script_path + @"\\forecast_plot.py", Encoding.UTF8))
            {
                forecast_plot = sr1.ReadToEnd();
            }

            string sc = "";
            sc += "import pandas as pd\r\n";
            sc += "import matplotlib\r\n";
            sc += "from matplotlib import pyplot as plt\r\n";
            sc += "from gluonts.distribution.multivariate_gaussian import MultivariateGaussianOutput\r\n";
            sc += "from gluonts.distribution.student_t import StudentTOutput\r\n";
            sc += "from gluonts.distribution.neg_binomial import NegativeBinomialOutput\r\n";
            sc += "from gluonts.distribution.piecewise_linear import PiecewiseLinearOutput\r\n";
            sc += "import sys\r\n";
            sc += "import numpy as np\r\n";

            sc += "freq_ = \"" + textBox1.Text + "\"\r\n";
            sc += "predict_length = " + numericUpDown3.Value.ToString() + "\r\n";
            sc += "seq_length = " + numericUpDown4.Value.ToString() + "\r\n";
            sc += "batch_size_ = " + numericUpDown6.Value.ToString() + "\r\n";
            sc += "num_layers_ = " + numericUpDown7.Value.ToString() + "\r\n";
            sc += "num_cells_ = " + numericUpDown8.Value.ToString() + "\r\n";
            sc += "epochs_ = " + numericUpDown5.Value.ToString() + "\r\n";

            sc += "df = pd.read_csv('tmp_deepAR_input.csv',index_col=0)\r\n";
            sc += "data_length = len(df)\r\n";
            sc += "\r\n";

            if (listBox3.SelectedIndices.Count > 0)
            {
                for (int i = 0; i < listBox3.SelectedIndices.Count; i++)
                {
                    sc += "feat" + (i + 1).ToString() + "= np.array(df." + listBox3.Items[listBox3.SelectedIndices[i]].ToString() + ").reshape((1,-1))\r\n";
                }
                sc += "features_real = np.concatenate([";
                for (int i = 0; i < listBox3.SelectedIndices.Count; i++)
                {
                    sc += "feat" + (i + 1).ToString();
                    if (i < listBox3.SelectedIndices.Count - 1) sc += ",";
                }
                sc += "], axis = 0)\r\n";
            }
            sc += "\r\n";

            sc += "df_train = pd.concat([";
            for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
            {
                sc += "df." + listBox1.Items[listBox1.SelectedIndices[i]].ToString() + "[:-predict_length]";
                if (i < listBox1.SelectedIndices.Count - 1) sc += ",";
            }
            sc += "], axis = 1)\r\n";
            sc += "\r\n";

            sc += "df_test = pd.concat([";
            for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
            {
                sc += "df." + listBox1.Items[listBox1.SelectedIndices[i]].ToString() + "[-(seq_length+predict_length):]";
                if (i < listBox1.SelectedIndices.Count - 1) sc += ",";
            }
            sc += "], axis = 1)\r\n";
            sc += "\r\n";
            sc += "pd.DataFrame(df_train).to_csv('train0.csv')\r\n";
            sc += "pd.DataFrame(df_test).to_csv('test0.csv')\r\n";
            sc += "\r\n";

            if (listBox1.SelectedIndices.Count > 1)
            {

                sc += "df_train = pd.DataFrame([";
                for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
                {
                    sc += "df." + listBox1.Items[listBox1.SelectedIndices[i]].ToString() + "[:-predict_length]";
                    if (i < listBox1.SelectedIndices.Count - 1) sc += ",";
                }
                sc += "])\r\n";
                sc += "\r\n";

                sc += "df_test = pd.DataFrame([";
                for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
                {
                    sc += "df." + listBox1.Items[listBox1.SelectedIndices[i]].ToString() + "[-(seq_length+predict_length):]";
                    if (i < listBox1.SelectedIndices.Count - 1) sc += ",";
                }
                sc += "])\r\n";
                sc += "\r\n";
            }
            else
            {
                sc += "df_train = df." + listBox1.Items[listBox1.SelectedIndices[0]].ToString() + "[:-predict_length]\r\n";
                sc += "df_test = df." + listBox1.Items[listBox1.SelectedIndices[0]].ToString() + "[-(seq_length+predict_length):]\r\n";
            }
            if (listBox3.SelectedIndices.Count > 0)
            {
                sc += "features_real_train = features_real[:,:-predict_length]\r\n";
                sc += "features_real_test = features_real[:, -(seq_length + predict_length):]\r\n";
            }
            sc += "\r\n";
            sc += "\r\n";
            sc += "context_length_ = seq_length\r\n";
            sc += "prediction_length_ = predict_length\r\n";

            sc += "index_len = len(df_train.index)\r\n";

            sc += "from gluonts.dataset.common import ListDataset\r\n";
            sc += "\r\n";
            sc += "\r\n";
            sc += "training_data = ListDataset(\r\n";
            sc += "    [{ \"start\": df.index[0], \"target\": df_train";
            if (listBox3.SelectedIndices.Count > 0)
            {
                sc += ",\"feat_dynamic_real\": features_real_train,";
            }
            sc += " }],\r\n";
            sc += "    freq = freq_\r\n";
            if (listBox1.SelectedIndices.Count > 1)
            {
                sc += ", one_dim_target = False)\r\n";
            }
            else
            {
                sc += ", one_dim_target = True)\r\n";
            }
            sc += "test_data = ListDataset(\r\n";
            sc += "    [{ \"start\": df.index[data_length-(seq_length+predict_length)], \"target\":df_test";
            if (listBox3.SelectedIndices.Count > 0)
            {
                sc += ",\"feat_dynamic_real\": features_real_test,";
            }
            sc += "} ],\r\n";
            sc += "    freq = freq_";
            if (listBox1.SelectedIndices.Count > 1)
            {
                sc += ", one_dim_target = False)\r\n";
            }
            else
            {
                sc += ", one_dim_target = True)\r\n";
            }
            sc += "\r\n";
            sc += "\r\n";
            sc += "dim = " + listBox1.SelectedIndices.Count.ToString() + "\r\n";
            sc += "import multiprocessing\r\n";

            sc += forecast_plot + "\r\n";

            return sc;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                button6_Click(sender, e);

                System.IO.Directory.SetCurrentDirectory(wrkdir);
                if (System.IO.File.Exists("tmp_deepARprediction1.png")) System.IO.File.Delete("tmp_deepARprediction1.png.png");
                pictureBox1.Image = null;

                train_mode = true;
                if (process_train == null && System.IO.File.Exists("train_finish.txt"))
                {
                    System.IO.File.Delete("train_finish.txt");
                }

                string sc = "";
                sc = deepar_common_code();

                sc += "\r\n";
                sc += "\r\n";
                sc += "from gluonts.model.deepar import DeepAREstimator\r\n";
                sc += "from gluonts.trainer import Trainer\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "estimator = DeepAREstimator(freq = freq_,\r\n";
                sc += "    prediction_length = prediction_length_,\r\n";
                sc += "    context_length = context_length_,\r\n";
                sc += "    trainer = Trainer(epochs = epochs_, batch_size = batch_size_, ctx = 'cpu'),\r\n";
                sc += "    num_layers = num_layers_,\r\n";
                sc += "    num_cells = num_cells_,\r\n";
                sc += "    use_feat_dynamic_real = ";
                if (listBox3.SelectedIndices.Count >= 1)
                {
                    sc += "True";
                }
                else
                {
                    sc += "False";
                }
                sc += ",\r\n";
                sc += "    # distr_output=StudentTOutput()\r\n";
                sc += "    # distr_output=NegativeBinomialOutput()\r\n";
                sc += "    # distr_output=PiecewiseLinearOutput()\r\n";
                if (listBox1.SelectedIndices.Count > 1)
                {
                    sc += "    distr_output = MultivariateGaussianOutput(dim = dim)\r\n";
                }
                sc += ")\r\n";
                sc += "predictor = estimator.train(training_data = training_data, num_workers = 1)\r\n";

                sc += "\r\n";
                sc += "\r\n";
                sc += "#export Trained model\r\n";
                sc += "from pathlib import Path\r\n";
                sc += "predictor.serialize(Path(\"./\"))\r\n";

                sc += "from gluonts.dataset.util import to_pandas\r\n";
                sc += "from gluonts.evaluation.backtest import make_evaluation_predictions\r\n";

                sc += "\r\n";
                sc += "\r\n";
                sc += "forecast_plot(0,dim, predictor, training_data, 'tmp_deepARprediction1.png')\r\n";
                sc += "#forecast_plot(0,dim, predictor, test_data, 'tmp_deepARprediction1.png')\r\n";

                sc += "\r\n";
                sc += "\r\n";
                sc += "import os\r\n";
                sc += "multiprocessing.freeze_support()\r\n";
                sc += "path_w = 'train_finish.txt'\r\n";

                sc += "s = 'New file'\r\n";
                sc += "with open(path_w, mode= 'w') as f:\r\n";
                sc += "    f.write(s)\r\n";

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter("train_deepAR.py", false, Encoding.UTF8))
                {
                    writer.Write(sc);
                }
                //return;

                app_train.FileName = python_venv + "\\python.exe";
                app_train.Arguments = " " + "train_deepAR.py";
                app_train.UseShellExecute = false;

                String envPath = Environment.GetEnvironmentVariable("Path");
                Environment.SetEnvironmentVariable("Path", PythonEnv);

                process_train = System.Diagnostics.Process.Start(app_train);
                timer1.Start();
            }
            catch
            {
                timer1.Stop();
            }
        }

        void processClose(System.Diagnostics.Process process)
        {
            if (process == null) return;

            bool cond = false;
            if (train_mode) cond = System.IO.File.Exists("train_finish.txt");
            else cond = System.IO.File.Exists("test_finish.txt");

            if (cond)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    //KillProcessTree(process);
                    if (train_mode)
                        process_train = null;
                    else
                        process_test = null;
                }
                timer1.Stop();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            processClose(process_train);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (train_mode)
            {
                processClose(process_train);
                if (System.IO.File.Exists("train_finish.txt"))
                {
                    if (System.IO.File.Exists("tmp_deepARprediction1.png"))
                    {
                        pictureBox1.Image = CreateImage("tmp_deepARprediction1.png");
                    }
                    System.IO.File.Delete("train_finish.txt");
                }
            }
            else
            {
                processClose(process_test);
                if (System.IO.File.Exists("test_finish.txt"))
                {
                    output_format();
                    System.IO.File.Delete("test_finish.txt");

                    if (System.IO.File.Exists("tmp_deepARprediction5.png"))
                    {
                        pictureBox1.Image = CreateImage("tmp_deepARprediction5.png");
                    }
                    agg_metrics();
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                System.IO.Directory.SetCurrentDirectory(wrkdir);
                if (System.IO.File.Exists("tmp_deepARprediction5.png")) System.IO.File.Delete("tmp_deepARprediction5.png");
                pictureBox1.Image = null;

                if (System.IO.File.Exists("tmp_deepAR_prediction.csv")) System.IO.File.Delete("tmp_deepAR_prediction.csv");
                if (System.IO.File.Exists("tmp_deepAR_prediction2.csv")) System.IO.File.Delete("tmp_deepAR_prediction2.csv");

                train_mode = false;
                if (process_test == null && System.IO.File.Exists("test_finish.txt"))
                {
                    System.IO.File.Delete("test_finish.txt");
                }

                string sc = "";
                sc = deepar_common_code();

                sc += "\r\n";
                sc += "\r\n";
                sc += "from gluonts.model.deepar import DeepAREstimator\r\n";
                sc += "#from gluonts.trainer import Trainer\r\n";
                sc += "from gluonts.model.predictor import Predictor\r\n";
                sc += "from pathlib import Path\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "#import Trained model\r\n";
                sc += "predictor = Predictor.deserialize(Path(\"./\"))\r\n";

                sc += "\r\n";
                sc += "\r\n";

                sc += "from gluonts.dataset.util import to_pandas\r\n";
                sc += "from gluonts.evaluation.backtest import make_evaluation_predictions\r\n";
                sc += "forecast_it, ts_it = make_evaluation_predictions(\r\n";
                sc += "   dataset= test_data,    # test dataset\r\n";
                sc += "   predictor= predictor,  # predictor\r\n";
                sc += "   num_samples= 100,      # number of sample paths we want for evaluation\r\n";
                sc += ")\r\n";

                sc += "tss = list(ts_it)\r\n";
                sc += "ts_entry = tss[0]\r\n";
                sc += "forecasts = list(forecast_it)\r\n";
                sc += "forecast_entry = forecasts[0]\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "print(f\"Number of sample paths: {forecast_entry.num_samples}\")\r\n";
                sc += "print(f\"Dimension of samples: {forecast_entry.samples.shape}\")\r\n";
                sc += "print(f\"Start date of the forecast window: {forecast_entry.start_date}\")\r\n";
                sc += "print(f\"Frequency of the time series: {forecast_entry.freq}\")\r\n";
                sc += "print(f\"Mean of the future window: {forecast_entry.mean}\")\r\n";
                sc += "print(f\"0.5-quantile (median) of the future window: {forecast_entry.quantile(0.5)}\")\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "#forecast_plot((seq_length+predict_length), dim, predictor, training_data, 'tmp_deepARprediction5.png')\r\n";
                sc += "forecast_plot(0,dim, predictor, test_data, 'tmp_deepARprediction5.png')\r\n";
                sc += "\r\n";
                sc += "\r\n";

                sc += "import numpy as np\r\n";
                sc += "pd.DataFrame(forecast_entry.mean).to_csv('deepAR_prediction.csv')\r\n";
                sc += "\r\n";
                sc += "from gluonts.evaluation import Evaluator\r\n";
                sc += "from gluonts.evaluation import MultivariateEvaluator\r\n";
                sc += "\r\n";
                sc += "\r\n";

                if (listBox1.SelectedIndices.Count == 1)
                {
                    sc += "evaluator = Evaluator(quantiles=[0.5], seasonality=1)\r\n";
                }
                else
                {
                    sc += "evaluator = MultivariateEvaluator(quantiles=[0.5], seasonality=1)\r\n";
                }
                sc += "agg_metrics, item_metrics = evaluator(iter(tss), iter(forecasts), num_series=len(test_data))\r\n";
                sc += "agg_metrics_df = pd.DataFrame.from_dict(agg_metrics, orient=\"index\")\r\n";
                sc += "agg_metrics_df.to_csv('agg_metrics.csv')\r\n";
                sc += "\r\n";
                sc += "#item_metrics.plot(x='MSIS', y='MASE', kind='scatter', c=item_metrics.index, cmap='Accent')\r\n";
                sc += "#plt.grid(which='both')\r\n";
                sc += "#plt.show()\r\n";
                sc += "\r\n";
                sc += "import os\r\n";
                sc += "multiprocessing.freeze_support()\r\n";
                sc += "path_w = 'test_finish.txt'\r\n";
                sc += "\r\n";
                sc += "s = 'New file'\r\n";
                sc += "with open(path_w, mode='w') as f:\r\n";
                sc += "    f.write(s)\r\n";

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter("test_deepAR.py", false, Encoding.UTF8))
                {
                    writer.Write(sc);
                }
                //return;

                app_test.FileName = python_venv + "\\python.exe";
                app_test.Arguments = " " + "test_deepAR.py";
                app_test.UseShellExecute = false;

                String envPath = Environment.GetEnvironmentVariable("Path");
                Environment.SetEnvironmentVariable("Path", PythonEnv);

                timer1.Start();
                process_test = System.Diagnostics.Process.Start(app_test);
            }
            catch
            {
                timer1.Stop();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            processClose(process_test);
            timer1.Stop();
        }

        void ListBoxReset()
        {
            System.IO.Directory.SetCurrentDirectory(wrkdir);

            if (System.IO.File.Exists("header.txt"))
            {
                System.IO.File.Delete("header.txt");
            }
            string args = " ";
            args += " " + InputFileName;
            Rscript(Script_path + "inputFileSelect.r", args);

            if (!System.IO.File.Exists("header.txt"))
            {
                return;
            }

            listBox1.Items.Clear();
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            System.IO.StreamReader sr = new System.IO.StreamReader("header.txt", Encoding.GetEncoding("SHIFT_JIS"));
            if (sr != null)
            {
                while (sr.EndOfStream == false)
                {
                    string line = sr.ReadLine().Replace("\n", "").Replace("\r", "");
                    listBox1.Items.Add(line);
                    listBox2.Items.Add(line);
                    listBox3.Items.Add(line);
                }
            }
            if (sr != null) sr.Close();

            if (listBox1.Items.Count >= 2)
            {
                listBox1.SelectedIndex = 1;
                listBox2.SelectedIndex = 0;
            }
            if (listBox1.Items.Count >= 3)
            {
                listBox3.SelectedIndex = 2;
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            System.IO.Directory.SetCurrentDirectory(wrkdir);
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
			ListBoxReset();
        }

        private void input_format()
        {
            System.IO.Directory.SetCurrentDirectory(wrkdir);

            string filename = InputFileName;
            filename = filename.Replace("\\", "\\\\");

            string wk = wrkdir.Replace("\\", "\\\\");
            string sc = "setwd(\"" + wk + "\")\r\n";
            sc += "df <- read.csv(\"" + filename + "\", header=T, stringsAsFactors = F, na.strings = c(\"\", \"NA\"))\r\n";

            sc += "df2 <- data.frame(df['" + listBox2.Items[listBox2.SelectedIndex].ToString() + "']";
            for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
            {
                sc += ",df['" + listBox1.Items[listBox1.SelectedIndices[i]].ToString() + "']";
            }
            for (int i = 0; i < listBox3.SelectedIndices.Count; i++)
            {
                sc += ",df['" + listBox3.Items[listBox3.SelectedIndices[i]].ToString() + "']";
            }
            sc += ")\r\n";
            sc += "colnames(df2)<-c('ds'";
            for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
            {
                sc += ",'" + listBox1.Items[listBox1.SelectedIndices[i]].ToString() + "'";
            }
            for (int i = 0; i < listBox3.SelectedIndices.Count; i++)
            {
                sc += ",'" + listBox3.Items[listBox3.SelectedIndices[i]].ToString() + "'";
            }
            sc += ")\r\n";

            sc += "write.csv(df2,\"" + wk + "\\\\tmp_deepAR_input.csv\",row.names = FALSE)\r\n";
            sc += "#Sys.sleep(100)\r\n";
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter("inputFmt.r", false, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.Write(sc);
            }
            Rscript("inputFmt.r", "");
        }

        private void output_format()
        {
            System.IO.Directory.SetCurrentDirectory(wrkdir);

            if (System.IO.File.Exists("header.txt"))
            {
                System.IO.File.Delete("header.txt");
            }

            string filename = openFileDialog1.FileName;
            filename = filename.Replace("\\", "\\\\");

            string wk = wrkdir.Replace("\\", "\\\\");
            string sc = "library(\"lubridate\")\r\n";

            sc += "setwd(\"" + wk + "\")\r\n";
            sc += "file_name <- \"tmp_deepAR_input.csv\"\r\n";
            sc += "seq_length <- " + numericUpDown4.Value.ToString() + "\r\n";

            sc += "df <- read.csv(file_name, header=T, stringsAsFactors = F, na.strings = c(\"\", \"NA\"))\r\n";
            sc += "df$ds<-as.Date(df$ds)\r\n";

            sc += "df_pred <- read.csv( \"deepAR_prediction.csv\", header=T, stringsAsFactors = F, na.strings = c(\"\", \"NA\"))\r\n";
            sc += "df_test <- read.csv(\"test0.csv\", header = T, stringsAsFactors = F, na.strings = c(\"\", \"NA\"))\r\n";
            sc += "df_train <- read.csv(\"train0.csv\", header = T, stringsAsFactors = F, na.strings = c(\"\", \"NA\"))\r\n";

            sc += "df_test$ds<-as.Date(df_test$ds)\r\n";
            sc += "df_train$ds<-as.Date(df_train$ds)\r\n";

            sc += "ds <- as.Date(df_test$ds[(seq_length + 1):nrow(df_test)])\r\n";
            sc += "df_prediction <- data.frame(ds)\r\n";

            for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
            {
                sc += "df_prediction<-cbind(df_prediction, data.frame(as.numeric(df_pred[," + (i + 2).ToString() + "])))\r\n";
            }
            sc += "colnames(df_prediction) <- colnames(df_train)\r\n";
            sc += "write.csv(df_prediction,\"tmp_deepAR_prediction.csv\",row.names = FALSE)\r\n";

            sc += "df_tot <-rbind(df_train, df_prediction)\r\n";
            sc += "write.csv(df_tot, \"tmp_deepAR_prediction2.csv\", row.names = FALSE)\r\n";
            sc += "#Sys.sleep(100)\r\n";

            using (System.IO.StreamWriter writer = new System.IO.StreamWriter("outputFmt.r", false, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.Write(sc);
            }
            Rscript("outputFmt.r", "");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.FileName == "")
            {
                return;
            }
            input_format();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox1.Dock = DockStyle.None;

                return;
            }
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Dock = DockStyle.Fill;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                Bitmap bmp = new Bitmap(pictureBox1.Image);
                Clipboard.SetImage(bmp);

                //後片付け
                bmp.Dispose();
            }
            catch
            {

            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBox3.Items.Count; i++)
            {
                bool s = false;
                for (int k = 0; k < listBox1.SelectedIndices.Count; k++)
                {
                    if (listBox1.SelectedIndices[k] == i)
                    {
                        listBox3.SetSelected(i, false);
                        s = true;
                    }
                }
                for (int k = 0; k < listBox2.SelectedIndices.Count; k++)
                {
                    if (listBox2.SelectedIndices[k] == i)
                    {
                        listBox3.SetSelected(i, false);
                        s = true;
                    }
                }
                if (!s)
                {
                    listBox3.SetSelected(i, true);
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBox3.Items.Count; i++)
            {
                listBox3.SetSelected(i, false);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (_ImageView == null) _ImageView = new ImageView();
            _ImageView.form1 = this.form1;
            string file = "tmp_deepARprediction1.png";
            if (!train_mode)
            {
                file = "tmp_deepARprediction1.png";
            }
            if (System.IO.File.Exists(file))
            {
                _ImageView.pictureBox1.ImageLocation = file;
                _ImageView.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                _ImageView.pictureBox1.Dock = DockStyle.Fill;
                _ImageView.Show();
            }
        }

        public static string FnameToDataFrameName(string fname, bool is_filename)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(fname);

            name = name.Replace("-", "_");
            name = name.Replace("+", "_");
            name = name.Replace("/", "_");
            name = name.Replace("*", "_");
            name = name.Replace("%", "_");
            name = name.Replace("=", "_");
            name = name.Replace("$", "_");
            name = name.Replace("~", "_");
            name = name.Replace("?", "_");
            name = name.Replace("{", "_");
            name = name.Replace("}", "_");
            name = name.Replace("(", "_");
            name = name.Replace(")", "_");
            name = name.Replace("[", "_");
            name = name.Replace("]", "_");
            name = name.Replace("&", "_");
            name = name.Replace("^", "_");
            name = name.Replace("`", "_");
            name = name.Replace("'", "_");
            name = name.Replace(".", "_");
            name = name.Replace(",", "_");
            name = name.Replace("@", "_");
            name = name.Replace("!", "_");
            name = name.Replace(":", "_");
            name = name.Replace(";", "_");
            name = name.Replace(" ", "_");
            name = name.Replace("　", "_");
            if (!is_filename) name = "i." + name;
            return name;
        }

        public void SelectionVarWrite_(ListBox list1, ListBox list2, string filename)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(filename, false, Encoding.GetEncoding("SHIFT_JIS"));
            if (sw != null)
            {
                sw.Write(list1.SelectedIndices.Count.ToString() + "\r\n");
                for (int i = 0; i < list1.SelectedIndices.Count; i++)
                {
                    sw.Write(list1.SelectedIndices[i].ToString());
                    sw.Write(",");
                    sw.Write(list1.Items[list1.SelectedIndices[i]].ToString() + "\r\n");
                }

                for (int i = 0; i < list2.SelectedIndices.Count; i++)
                {
                    sw.Write(list2.SelectedIndices[i].ToString());
                    sw.Write(",");
                    sw.Write(list2.Items[list2.SelectedIndices[i]].ToString() + "\r\n");
                }
                sw.Close();
            }
        }

        public static void VarAutoSelection_(ListBox list1, ListBox list2, string filename)
        {
            if (!System.IO.File.Exists(filename))
            {
                MessageBox.Show("目的変数を決定して下さい");
                return;
            }

            ListBox tmp1 = list1;
            ListBox tmp2 = list2;

            System.IO.StreamReader sr = null;
            try
            {
                sr = new System.IO.StreamReader(filename, Encoding.GetEncoding("SHIFT_JIS"));
                if (sr != null)
                {
                    list1.SelectedIndices.Clear();
                    list2.SelectedIndices.Clear();

                    while (sr.EndOfStream == false)
                    {
                        string line = sr.ReadLine();
                        int n = int.Parse(line);
                        for (int i = 0; i < n; i++)
                        {
                            line = sr.ReadLine();
                            var s = line.Split(',');
                            int index = int.Parse(s[0]);
                            list1.SelectedIndices.Add(index);
                        }
                        break;
                    }

                    while (sr.EndOfStream == false)
                    {
                        string line = sr.ReadLine();
                        var s = line.Split(',');
                        int index = int.Parse(s[0]);
                        list2.SelectedIndices.Add(index);
                    }
                    sr.Close();
                }
            }
            catch
            {
                if (sr != null) sr.Close();
                list1 = tmp1;
                list2 = tmp2;
            }
        }

        public void load_model(string modelfile, object sender, EventArgs e)
        {
            System.IO.File.Copy(modelfile, "prediction_net-0000.params", true);
            System.IO.File.Copy(modelfile + "." + "type.txt", "type.txt", true);
            System.IO.File.Copy(modelfile + "." + "prediction_net-network.json", "prediction_net-network.json", true);
            System.IO.File.Copy(modelfile + "." + "input_transform.json", "input_transform.json", true);
            System.IO.File.Copy(modelfile + "." + "parameters.json", "parameters.json", true);
            System.IO.File.Copy(modelfile + "." + "version.json", "version.json", true);

            if (System.IO.File.Exists(modelfile + "." + "agg_metrics.csv"))
            {
                System.IO.File.Copy(modelfile + "." + "agg_metrics.csv", "agg_metrics.csv", true);
                agg_metrics();
            }
            //
            VarAutoSelection_(listBox1, listBox2, modelfile + ".select_variables.dat");
            VarAutoSelection_(listBox3, listBox3, modelfile + ".select_variables2.dat");

            System.IO.StreamReader sr = new System.IO.StreamReader(modelfile + ".options", Encoding.GetEncoding("SHIFT_JIS"));
            if (sr != null)
            {
                while (sr.EndOfStream == false)
                {
                    string s = sr.ReadLine();
                    var ss = s.Split(',');
                    if (ss[0].IndexOf("freq") >= 0)
                    {
                        textBox1.Text = ss[1].Replace("\r\n", "");
                        continue;
                    }
                    if (ss[0].IndexOf("prediction") >= 0)
                    {
                        numericUpDown3.Value = decimal.Parse(ss[1].Replace("\r\n", ""));
                        continue;
                    }
                    if (ss[0].IndexOf("context_length") >= 0)
                    {
                        numericUpDown4.Value = decimal.Parse(ss[1].Replace("\r\n", ""));
                        continue;
                    }
                    if (ss[0].IndexOf("epochs") >= 0)
                    {
                        numericUpDown5.Value = decimal.Parse(ss[1].Replace("\r\n", ""));
                        continue;
                    }
                    if (ss[0].IndexOf("batch_size") >= 0)
                    {
                        numericUpDown6.Value = decimal.Parse(ss[1].Replace("\r\n", ""));
                        continue;
                    }
                    if (ss[0].IndexOf("num_layers") >= 0)
                    {
                        numericUpDown7.Value = decimal.Parse(ss[1].Replace("\r\n", ""));
                        continue;
                    }
                    if (ss[0].IndexOf("num_cells") >= 0)
                    {
                        numericUpDown8.Value = decimal.Parse(ss[1].Replace("\r\n", ""));
                        continue;
                    }
                }
                sr.Close();
            }
            this.TopMost = true;
            this.TopMost = false;
        }

        string clipValueTxt(string value)
        {
            float v = float.Parse(value) * 1000.0f;
            int i = (int)v;
            v = (float)i / 1000.0f;

            return v.ToString();
        }
        void agg_metrics()
        {
            rmse = "unknown";
            MSE = "unknown";
            MASE = "unknown";
            MAPE = "unknown";
            NRMSE = "unknown";

            if (System.IO.File.Exists(wrkdir + "\\agg_metrics.csv"))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(wrkdir + "\\agg_metrics.csv", Encoding.GetEncoding("SHIFT_JIS"));
                if (sr != null)
                {
                    while (sr.EndOfStream == false)
                    {
                        string s = sr.ReadLine();
                        var ss = s.Split(',');
                        if (ss.Length == 0) continue;

                        if (ss[0].IndexOf("RMSE") >= 0)
                        {
                            rmse = ss[1].Replace("\r", "").Replace("\r\n", "");
                            label2.Text = "RMSE = " + clipValueTxt(rmse);
                        }
                        if (ss[0].IndexOf("MSE") >= 0)
                        {
                            MSE = ss[1].Replace("\r", "").Replace("\r\n", "");
                            label3.Text = "MSE = " + clipValueTxt(MSE);
                        }
                        if (ss[0].IndexOf("MASE") >= 0)
                        {
                            MASE = ss[1].Replace("\r", "").Replace("\r\n", "");
                            label12.Text = "MASE = " + clipValueTxt(MASE);
                        }
                        if (ss[0].IndexOf("MAPE") >= 0)
                        {
                            MAPE = ss[1].Replace("\r", "").Replace("\r\n", "");
                            label13.Text = "MAPE = " + clipValueTxt(MAPE);
                        }
                        if (ss[0].IndexOf("NRMSE") >= 0)
                        {
                            NRMSE = ss[1].Replace("\r", "").Replace("\r\n", "");
                            label15.Text = "NRMSE = " + clipValueTxt(NRMSE);
                        }
                    }
                    sr.Close();
                }
            }
        }
        private void save_mocel()
        {
            if (timer1.Enabled) return;

            agg_metrics();
            string model_id = DateTime.Now.ToLongDateString() + DateTime.Now.ToShortTimeString().Replace(":", "_");

            string id = FnameToDataFrameName(model_id, true);

            if (!System.IO.Directory.Exists("model"))
            {
                System.IO.Directory.CreateDirectory("model");
            }

            bool update = true;
            string save_name = wrkdir + "\\model\\prediction_net-0000.params(MSE=" + MSE + ")" + id;

            if (System.IO.File.Exists(save_name))
            {
                if (MessageBox.Show("The same model exists", "overwrite?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    update = false;
                }
            }
            if (update)
            {
                if (update)
                {

                    if (System.IO.File.Exists(wrkdir + "\\prediction_net-0000.params"))
                    {
                        System.IO.File.Copy(wrkdir + "\\prediction_net-0000.params", save_name, true);
                    }
                    if (System.IO.File.Exists(wrkdir + "\\prediction_net-network.json"))
                    {
                        System.IO.File.Copy(wrkdir + "\\prediction_net-network.json", save_name + "." + "prediction_net-network.json", true);
                    }
                    if (System.IO.File.Exists(wrkdir + "\\type.txt"))
                    {
                        System.IO.File.Copy(wrkdir + "\\type.txt", save_name + "." + "type.txt", true);
                    }
                    if (System.IO.File.Exists(wrkdir + "\\input_transform.json"))
                    {
                        System.IO.File.Copy(wrkdir + "\\input_transform.json", save_name + "." + "input_transform.json", true);
                    }
                    if (System.IO.File.Exists(wrkdir + "\\parameters.json"))
                    {
                        System.IO.File.Copy(wrkdir + "\\parameters.json", save_name + "." + "parameters.json", true);
                    }
                    if (System.IO.File.Exists(wrkdir + "\\version.json"))
                    {
                        System.IO.File.Copy(wrkdir + "\\version.json", save_name + "." + "version.json", true);
                    }
                    if (System.IO.File.Exists(wrkdir + "\\agg_metrics.csv"))
                    {
                        System.IO.File.Copy(wrkdir + "\\agg_metrics.csv", save_name + "." + "agg_metrics.csv", true);
                    }
                    SelectionVarWrite_(listBox1, listBox2, save_name + ".select_variables.dat");
                    SelectionVarWrite_(listBox3, listBox3, save_name + ".select_variables2.dat");

                    {
                        System.IO.StreamWriter sw = new System.IO.StreamWriter(save_name + ".options", false, Encoding.GetEncoding("SHIFT_JIS"));
                        if (sw != null)
                        {
                            sw.Write("freq,");
                            sw.Write(textBox1.Text + "\r\n");
                            sw.Write("prediction,");
                            sw.Write(numericUpDown3.Value.ToString() + "\r\n");
                            sw.Write("context_length ,");
                            sw.Write(numericUpDown4.Value.ToString() + "\r\n");
                            sw.Write("epochs ,");
                            sw.Write(numericUpDown5.Value.ToString() + "\r\n");
                            sw.Write("batch_size ,");
                            sw.Write(numericUpDown6.Value.ToString() + "\r\n");
                            sw.Write("num_layers ,");
                            sw.Write(numericUpDown7.Value.ToString() + "\r\n");
                            sw.Write("num_cells ,");
                            sw.Write(numericUpDown8.Value.ToString() + "\r\n");
                            sw.Close();
                        }
                    }
                }
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            save_mocel();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled) return;
            openFileDialog1.InitialDirectory = wrkdir + "\\model";
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            load_model(openFileDialog1.FileName, sender, e);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                button6_Click(sender, e);

                System.IO.Directory.SetCurrentDirectory(wrkdir);
                if (!System.IO.File.Exists("tmp_deepARprediction1.png")) return;

                if (System.IO.File.Exists("tmp_deepARprediction2.png")) System.IO.File.Delete("tmp_deepARprediction2.png");
                pictureBox1.Image = null;

                train_mode = true;
                if (process_train == null && System.IO.File.Exists("train_finish.txt"))
                {
                    System.IO.File.Delete("train_finish.txt");
                }

                string sc = "";
                sc = deepar_common_code();

                sc += "\r\n";
                sc += "\r\n";
                sc += "from gluonts.dataset.util import to_pandas\r\n";
                sc += "from gluonts.evaluation.backtest import make_evaluation_predictions\r\n";
                sc += "from gluonts.model.predictor import Predictor\r\n";
                sc += "from pathlib import Path\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "#import Trained model\r\n";
                sc += "predictor = Predictor.deserialize(Path(\"./\"))\r\n";

                sc += "\r\n";
                sc += "\r\n";

                sc += "from gluonts.dataset.util import to_pandas\r\n";
                sc += "from gluonts.evaluation.backtest import make_evaluation_predictions\r\n";
                sc += "forecast_it, ts_it = make_evaluation_predictions(\r\n";
                sc += "   dataset= training_data,    # test dataset\r\n";
                sc += "   predictor= predictor,  # predictor\r\n";
                sc += "   num_samples= 100,      # number of sample paths we want for evaluation\r\n";
                sc += ")\r\n";

                sc += "tss = list(ts_it)\r\n";
                sc += "ts_entry = tss[0]\r\n";
                sc += "forecasts = list(forecast_it)\r\n";
                sc += "forecast_entry = forecasts[0]\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "print(f\"Number of sample paths: {forecast_entry.num_samples}\")\r\n";
                sc += "print(f\"Dimension of samples: {forecast_entry.samples.shape}\")\r\n";
                sc += "print(f\"Start date of the forecast window: {forecast_entry.start_date}\")\r\n";
                sc += "print(f\"Frequency of the time series: {forecast_entry.freq}\")\r\n";
                sc += "print(f\"Mean of the future window: {forecast_entry.mean}\")\r\n";
                sc += "print(f\"0.5-quantile (median) of the future window: {forecast_entry.quantile(0.5)}\")\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "forecast_plot(0,dim, predictor, training_data, 'tmp_deepARprediction2.png')\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "import os\r\n";
                sc += "multiprocessing.freeze_support()\r\n";
                sc += "path_w = 'train_finish.txt'\r\n";

                sc += "s = 'New file'\r\n";
                sc += "with open(path_w, mode= 'w') as f:\r\n";
                sc += "    f.write(s)\r\n";

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter("train_deepAR_plt.py", false, Encoding.UTF8))
                {
                    writer.Write(sc);
                }
                //return;

                app_train.FileName = python_venv + "\\python.exe";
                app_train.Arguments = " " + "train_deepAR_plt.py";
                app_train.UseShellExecute = false;

                String envPath = Environment.GetEnvironmentVariable("Path");
                Environment.SetEnvironmentVariable("Path", PythonEnv);

                process_train = System.Diagnostics.Process.Start(app_train);
                timer1.Start();
            }
            catch
            {
                timer1.Stop();
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                button6_Click(sender, e);

                System.IO.Directory.SetCurrentDirectory(wrkdir);
                if (!System.IO.File.Exists("tmp_deepARprediction5.png")) return;

                if (System.IO.File.Exists("tmp_deepARprediction6.png")) System.IO.File.Delete("tmp_deepARprediction6.png");
                pictureBox1.Image = null;

                train_mode = false;
                if (process_train == null && System.IO.File.Exists("test_finish.txt"))
                {
                    System.IO.File.Delete("test_finish.txt");
                }

                string sc = "";
                sc = deepar_common_code();

                sc += "\r\n";
                sc += "\r\n";
                sc += "from gluonts.dataset.util import to_pandas\r\n";
                sc += "from gluonts.evaluation.backtest import make_evaluation_predictions\r\n";
                sc += "from gluonts.model.predictor import Predictor\r\n";
                sc += "from pathlib import Path\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "#import Trained model\r\n";
                sc += "predictor = Predictor.deserialize(Path(\"./\"))\r\n";

                sc += "\r\n";
                sc += "\r\n";

                sc += "from gluonts.dataset.util import to_pandas\r\n";
                sc += "from gluonts.evaluation.backtest import make_evaluation_predictions\r\n";
                sc += "forecast_it, ts_it = make_evaluation_predictions(\r\n";
                sc += "   dataset= test_data,    # test dataset\r\n";
                sc += "   predictor= predictor,  # predictor\r\n";
                sc += "   num_samples= 100,      # number of sample paths we want for evaluation\r\n";
                sc += ")\r\n";

                sc += "tss = list(ts_it)\r\n";
                sc += "ts_entry = tss[0]\r\n";
                sc += "forecasts = list(forecast_it)\r\n";
                sc += "forecast_entry = forecasts[0]\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "print(f\"Number of sample paths: {forecast_entry.num_samples}\")\r\n";
                sc += "print(f\"Dimension of samples: {forecast_entry.samples.shape}\")\r\n";
                sc += "print(f\"Start date of the forecast window: {forecast_entry.start_date}\")\r\n";
                sc += "print(f\"Frequency of the time series: {forecast_entry.freq}\")\r\n";
                sc += "print(f\"Mean of the future window: {forecast_entry.mean}\")\r\n";
                sc += "print(f\"0.5-quantile (median) of the future window: {forecast_entry.quantile(0.5)}\")\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "forecast_plot(0,dim, predictor, test_data, 'tmp_deepARprediction6.png')\r\n";
                sc += "\r\n";
                sc += "\r\n";
                sc += "import os\r\n";
                sc += "multiprocessing.freeze_support()\r\n";
                sc += "path_w = 'test_finish.txt'\r\n";

                sc += "s = 'New file'\r\n";
                sc += "with open(path_w, mode= 'w') as f:\r\n";
                sc += "    f.write(s)\r\n";

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter("test_deepAR_plt.py", false, Encoding.UTF8))
                {
                    writer.Write(sc);
                }
                //return;

                app_train.FileName = python_venv + "\\python.exe";
                app_train.Arguments = " " + "test_deepAR_plt.py";
                app_train.UseShellExecute = false;

                String envPath = Environment.GetEnvironmentVariable("Path");
                Environment.SetEnvironmentVariable("Path", PythonEnv);

                process_test = System.Diagnostics.Process.Start(app_train);
                timer1.Start();
            }
            catch {
                timer1.Stop();
            }
        }
    }
}