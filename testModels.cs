//引入OpencvSharp和Dnn模块
using HB;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TensorFlow;
using HB.FormSettings;
using System.Threading.Tasks;



namespace 数据采集
{
    public partial class testModels : Form
    {
        private ScreenAdaptationUtility sau;
        public testModels()

        {
            InitializeComponent();
            ScreenAdaptationUtility.SetTag(this);
            sau = new ScreenAdaptationUtility(this);

        }


        //modelFile为.pb训练文件路径

        //private void button1_Click(object sender, EventArgs e)
        //{
        //    //获取当前应用程序的目录
        //    string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        //    //获取模型文件的相对路径
        //    string modelsPath = Path.Combine("..", "Models", "save_models.pb");

        //    ////构建完整的模型文件路径
        //    string modelFullPath = Path.Combine(currentDirectory, modelsPath);

        //    string modelPath = "D:\\Desktop\\Models\\zfnet_model.h5";

        //    string modelPath2 = "D:\\Desktop\\Models\\saved_model\\saved_model.pb";
        //    try
        //    {
        //        if (File.Exists(modelPath))
        //        {
        //            // 创建输入数据的形状和类型
        //            int batchSize = 1;
        //            int height = 100;
        //            int width = 100;
        //            int channels = 3;

        //            var input_data_shape = new long[] { batchSize, height, width, channels };
        //            var input_data_type = TFDataType.Float;

        //            // 创建一个形状为 (1, 100, 100, 3) 的随机输入数据
        //            float[,,,] input_data = GenerateRandomInputData(batchSize, height, width, channels);

        //            using (var graph = new TFGraph())
        //            {
        //                // 从模型文件中加载计算图
        //                graph.Import(File.ReadAllBytes(modelPath2));

        //                using (var sess = new TFSession(graph))
        //                {
        //                    var runner = sess.GetRunner();
        //                    runner.AddInput(graph["Cast_1"][0], new TFTensor(input_data, input_data_shape, input_data_type));
        //                    var r = runner.Run(graph.Softmax(graph["softmax_linear/softmax_linear"][0]));
        //                    var v = (float[,])r.GetValue();
        //                    Console.WriteLine(v[0, 0]);
        //                    Console.WriteLine(v[0, 1]);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("模型文件不存在");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("模型加载失败：" + ex.Message);
        //    }

        //}
        float[,,,] GenerateRandomInputData(int batchSize, int height, int width, int channels)
        {
            float[,,,] inputData = new float[batchSize, height, width, channels];
            Random random = new Random();

            for (int b = 0; b < batchSize; b++)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            inputData[b, h, w, c] = (float)random.NextDouble();
                        }
                    }
                }
            }
            return inputData;
        }

        private void testModels_Load(object sender, EventArgs e)
        {
            HB.FormSetting.CenterFormOnScreen(this);
            this.Resize += resize;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SqlConnection conn = null;
            conn = HB.SQLserver.getSqlConnection();
            conn.Open();
            if (conn != null)
            {
                MessageBox.Show("数据库连接成功！");
            }
            string selectStr = "select count(*) as totalRecord from table_1";
            DataSet ds = new DataSet();
            ds = HB.SQLserver.getDataSet(selectStr);
            DataTable dt = ds.Tables[0];
            DataRow dr = dt.Rows[0];
            string total_records = dr["totalRecord"].ToString();
            //MessageBox.Show(dr[])

            string connstring = "Data Source=.;Initial Catalog=3DAction;User ID=sa;Password=20021224;";
            ds = SQLserver.getDataSet(selectStr, connstring);
            dt = ds.Tables[0];
            dr = dt.Rows[0];
            total_records = dr["totalRecord"].ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string pythonPath = @"C:\Users\pc\AppData\Local\Programs\Python\Python311\python.exe"; // Python解释器路径
            string pythonScript = @"D:\Desktop\hello.py";  // Python文件的路径
            //string argument = "your_argument";  // 传递的参数
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"{pythonScript}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();//读取py输出的内容或者return的内容
                process.WaitForExit();
                MessageBox.Show(output);
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            string pythonPath = @"C:\Users\pc\AppData\Local\Programs\Python\Python311\python.exe"; // Python解释器路径
            //找pos文件路径
            string startPath = Application.StartupPath;
            string[] rootPath = startPath.Split(new string[] { "数据采集" }, StringSplitOptions.None);
            string posfileFolder = rootPath[0] + @"数据采集\数据采集\Resources\Data\falldown\202114181017575"; //在这里改变预测文件的路径
            string posfilePath = System.IO.Path.Combine(posfileFolder, "pos.txt");//pos。txt文件路径
            if (!File.Exists(posfilePath))
            {
                MessageBox.Show("pos.txt文件路径不存在");
                return;
            }
            //找Python脚本路径
            string Modelfolder = rootPath[0] + @"数据采集\数据采集\Resources\Model";
            string pythonScriptPath = System.IO.Path.Combine(Modelfolder, "mytest.py");//py运行文件路径

            string outputdata = await ProcessPool.RunPythonScriptAsync(pythonPath, pythonScriptPath, posfilePath);

            MessageBox.Show(outputdata);

        }
        private async Task RunPythonScriptAsync(string pythonScriptPath, string posfilePath)
        {
            string pythonPath = @"C:\Users\pc\AppData\Local\Programs\Python\Python311\python.exe"; // Python解释器路径
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo(pythonPath);
            startInfo.Arguments = $"{pythonScriptPath} {posfilePath}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                process.StartInfo = startInfo;
                process.Start();
                button6.PerformClick();
                // 异步读取输出
                string output = await process.StandardOutput.ReadToEndAsync();
                MessageBox.Show(output);
                process.WaitForExit();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
        private void resize(object sender, EventArgs e)
        {
            //sau.AdaptToScreenResolution();
        }

        private void testModels_Resize(object sender, EventArgs e)
        {
            if (sau != null)
                sau.AdaptToScreenResolution();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var graph = new TFGraph();
            var session = new TFSession(graph);

            // 指定模型的文件路径
            string modelFilePath = "path_to_your_model.pb";

            // 从文件中加载模型
            graph.Import(File.ReadAllBytes(modelFilePath));

            // 创建输入张量
            var inputTensor = graph["input_tensor_name"][0];

            // 创建输出张量
            var outputTensor = graph["output_tensor_name"][0];


            int batchSize = 100;
            int max_length = 80;

            double[][][] inputArray = new double[batchSize][][];

            // 填充 inputArray 数组，这里只是示例，根据你的数据结构进行填充
            for (int i = 0; i < batchSize; i++)
            {
                inputArray[i] = new double[max_length][];
                for (int j = 0; j < max_length; j++)
                {
                    inputArray[i][j] = new double[75];
                    for (int k = 0; k < 75; k++)
                    {
                        inputArray[i][j][k] = 1; // 替换为实际的数据值
                    }
                }
            }

            // 创建 TFTensor 对象
            TFTensor inputData = new TFTensor(inputArray);

            // 创建运行图
            var runner = session.GetRunner();

            // 设置输入张量的值
            runner.AddInput(inputTensor, inputData);

            // 运行模型
            var outputData = runner.Run();

            // 处理模型输出
            // outputData 是一个 TFTensor，你可以根据模型的需要进一步处理它


        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private async void button6_Click(object sender, EventArgs e)
        {


            // 将结果显示在 TextBox 中，逐渐增加数字
            for (int i = 0; i <= 1000000; i++)
            {
                textBox1.Text = i.ToString();
                await Task.Delay(1);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }

}