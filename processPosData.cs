using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Emgu.CV.Structure;
using TensorFlow;
using NumSharp;
using OpenCvSharp.Dnn;
using Microsoft.ML.Transforms;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;
using HBUtils;
using HBUtils.FormSet;

namespace 数据采集
{
    public partial class processPosData : Form
    {
        static string outputFolder = "";  //文件保存路径
        static string posFilePath = "";//得到pos文件路径


        public processPosData()
        {
            InitializeComponent();
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            posFilePath = filePahBox.Text;//得到pos文件路径
            if(string.IsNullOrEmpty(posFilePath))
            {
                MessageBox.Show("未选择要处理的pos文件");
                return;
            }
            double[,,] X_train = dataTreating(posFilePath);
            List<byte[,,]> X = dataConversion(X_train);
            MessageBox.Show("数据处理完毕");
            //Emgu.CV.Mat[] inputData = normalizeInputData(X);
            predictModel();
        }
        string modelFileDir = @"D:\Desktop\Models3\zfnet_model";
        private void predictModel()
        {

        }
        public class MovieReview
        {
            public string ReviewText { get; set; }
        }
        private void loadModel()
        {
            // 指定 SavedModel 目录路径
            string modelFilepath = modelFileDir + "\\saved_model.pb";
            string modelPath = @"D:\Desktop\Models3\zfnet_model.h5";

            MLContext mlContext = new MLContext();
            try
            {
                TensorFlowModel tensorFlowModel = mlContext.Model.LoadTensorFlowModel(modelFileDir);
                DataViewSchema schema = tensorFlowModel.GetModelSchema();
                // 获取模型的输入架构
                DataViewSchema inputSchema = tensorFlowModel.GetInputSchema();
                // 输出每个输入列的信息
                foreach (var column in inputSchema)
                {
                    MessageBox.Show($"Input Column Name: {column.Name}, Type: {column.Type}");
                }
                MessageBox.Show(" =============== TensorFlow Model Schema =============== ");
                MessageBox.Show("" + schema);
                var featuresType = (VectorDataViewType)schema["Features"].Type;
                MessageBox.Show($"Name: Features, Type: {featuresType.ItemType.RawType}, Size: ({featuresType.Dimensions[0]})");
                var predictionType = (VectorDataViewType)schema["Prediction/Softmax"].Type;
                MessageBox.Show($"Name: Prediction/Softmax, Type: {predictionType.ItemType.RawType}, Size: ({predictionType.Dimensions[0]})");

            }
            catch (Exception ex)
            {
                MessageBox.Show("" + ex.Message.ToString());
            }

            if (File.Exists(modelFilepath) != true)
            {
                MessageBox.Show("无效的文件路径");
            }
            Net net = CvDnn.ReadNetFromTensorflow(modelFilepath);//加载模型
            if (net.Empty())
            {
                MessageBox.Show("pd文件错误");
                return;
            }

            // 创建 TensorFlow.Session
            //using (var session = new TFSession(graph))
            //{
            //    // 替换为实际的输入数据
            //    var inputArray = np.load(@"D:\Desktop\Models3\Testfile\falldown\20211595415229\pos\djmis.npy");

            //    // 进行预测
            //    var runner = session.GetRunner();

            //    TFTensor tfInput = createTensor();

            //    runner.AddInput(graph["conv2d_1_input"][0], tfInput);
            //    runner.Fetch(graph["activation_5/Sigmoid"][0]);
            //    var output = runner.Run();

            //    // 获取预测结果
            //    var outputData = output[0].GetValue() as float[,,];

            //    // 输出结果（这里仅是示例）
            //    MessageBox.Show("模型输出:");
            //    MessageBox.Show("" + outputData);
            //}
        }
        private TFTensor createTensor()
        {
            var npArray = np.load(@"D:\Desktop\Models3\Testfile\falldown\20211595415229\pos\djmis.npy");
            var matrix = new float[1, 100, 100, 3];

            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    matrix[0, i, j, 0] = npArray[0][i][j][0];
                    matrix[0, i, j, 1] = npArray[0][i][j][1];
                    matrix[0, i, j, 2] = npArray[0][i][j][2];
                }
            }
            TFTensor tensor = matrix;
            return tensor;
        }

        //归一化数据
        private Emgu.CV.Mat[] normalizeInputData(Emgu.CV.Mat[] X)
        {
            // 归一化每个 Mat 范围为（0-1）
            for (int i = 0; i < X.Length; i++)
            {
                Emgu.CV.CvInvoke.Normalize(X[i], X[i], 0, 255, Emgu.CV.CvEnum.NormType.MinMax, DepthType.Cv8U);
            }
            return X;
        }
        private double[,,] dataTreating(string filePath)
        {
            // 替换为文件路径
            int max_length = 100; // 截取的最大帧数
            List<List<double>> coords = new List<List<double>>();
            List<List<List<double>>> X = new List<List<List<double>>>();//存取pos里面的所有数字信息
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    List<double> coordstr = ExtractNumbersFromLine(line);
                    coords.Add(coordstr);
                }
                while (coords.Count < max_length)
                {
                    List<double> zeroCoords = new List<double>();
                    for (int i = 0; i < 75; i++) // 假设每个坐标数据集包含 75 个元素
                    {
                        zeroCoords.Add(0.0); // 填充为零
                    }
                    coords.Add(zeroCoords);
                }
                X.Add(coords);
                int max_coords_length = 0;
                foreach (List<List<double>> coordsList in X)
                {
                    foreach (List<double> i in coordsList)
                    {
                        int coordsLength = i.Count;
                        if (coordsLength > max_coords_length)
                        {
                            max_coords_length = coordsLength;
                        }
                    }
                }
                for (int i = 0; i < X.Count; i++)
                {
                    for (int j = 0; j < X[i].Count; j++)
                    {
                        int currentLength = X[i][j].Count;
                        int lengthDifference = max_coords_length - currentLength;
                        for (int k = 0; k < lengthDifference; k++)
                        {
                            X[i][j].Add(0.0);
                        }
                    }
                }
            }
            // 创建一个多维数组并填充数据
            double[,,] X_new = new double[X.Count, max_length, 75];
            for (int i = 0; i < X.Count; i++)
            {
                for (int j = 0; j < max_length; j++)
                {
                    for (int k = 0; k < 75; k++)
                    {
                        X_new[i, j, k] = X[i][j][k];
                    }
                }
            }
            return X_new;
            // X_new 现在包含了填充后的数据
        }
        static List<double> ExtractNumbersFromLine(string line)
        {
            List<double> coords = new List<double>();
            string pattern = @"[-+]?\d*\.\d+E[-+]?\d+|[-+]?\d+\.\d+|[-+]?\d+";

            MatchCollection matches = Regex.Matches(line, pattern);

            foreach (Match match in matches)
            {
                if (double.TryParse(match.Value, out double number))
                {
                    coords.Add(number);
                }
            }
            if (coords.Count > 0)
            {
                coords.RemoveAt(0);//去除第一个数字----即帧记录
            }
            return coords;
        }
        // 现在，X 包含了从文件中提取的数字数据，每个子列表的长度为 max_length。
        #region data_conversion

        private List<byte[,,]> dataConversion(double[,,] X_train_normalized)
        {
            // 创建一个空的列表
            List<byte[,,]> djmiImages = new List<byte[,,]>();


            //得到pos.txt 文件的根目录，StringSplitOptions.None表示不移出空的字符串
            string outputFolder = posFilePath.Split(new string[] { "pos" }, StringSplitOptions.None)[0];
            //# 将 X_train 中的数据归一化到区间 (-255, 255)
            double minNormalizedValue = X_train_normalized.Cast<double>().Min();
            // 将 X_train 中的所有元素减去最小值，然后加 1 并且计算标准差log归一化
            double maxLogValue;
            double minLogValue;
            for (int i = 0; i < X_train_normalized.GetLength(0); i++)
            {
                for (int j = 0; j < X_train_normalized.GetLength(1); j++)
                {
                    for (int k = 0; k < X_train_normalized.GetLength(2); k++)
                    {
                        double temp = X_train_normalized[i, j, k] - minNormalizedValue + 1;
                        X_train_normalized[i, j, k] = Math.Log(temp);
                    }
                }
            }
            maxLogValue = X_train_normalized.Cast<double>().Max();
            minLogValue = X_train_normalized.Cast<double>().Min();
            for (int i = 0; i < X_train_normalized.GetLength(0); i++)
            {
                for (int j = 0; j < X_train_normalized.GetLength(1); j++)
                {
                    for (int k = 0; k < X_train_normalized.GetLength(2); k++)
                    {
                        double value = X_train_normalized[i, j, k];
                        // 进行数学操作
                        double newValue = ((value - minLogValue) / (maxLogValue - minLogValue) * 512) - 256;
                        // 更新数组元素
                        X_train_normalized[i, j, k] = newValue;
                    }
                }
            }
            int rgbScale = 10; // 替换为所需的 RGB 缩放因子
            for (int i = 0; i < X_train_normalized.GetLength(0); i++)
            {
                double[,] frameData = new double[X_train_normalized.GetLength(1), X_train_normalized.GetLength(2)];

                // 提取当前切片的数据
                for (int j = 0; j < X_train_normalized.GetLength(1); j++)
                {
                    for (int k = 0; k < X_train_normalized.GetLength(2); k++)
                    {
                        frameData[j, k] = X_train_normalized[i, j, k];
                    }
                }//framedata (-255~255)
                byte[,,] djmiImage = GenerateDjmiImage(frameData, rgbScale);
                //将字节流转为bitmap
                // 获取数组的维度
                int width = djmiImage.GetLength(0);
                int height = djmiImage.GetLength(1);
                int channels = djmiImage.GetLength(2);

                // 创建一个Bitmap对象
                Bitmap image = new Bitmap(width, height);

                // 将byte[,,]数组中的数据复制到Bitmap对象中
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color color = Color.FromArgb(djmiImage[x, y, 2], djmiImage[x, y, 1], djmiImage[x, y, 0]);//注意 这里读取djmi的三个通道对应顺序为bgr
                        image.SetPixel(y, x, color);//控制djmi泳道的方向
                    }
                }
                //保存djmi图片
                if (!Directory.Exists(Path.Combine(outputFolder, "djmi")))
                {
                    Directory.CreateDirectory(Path.Combine(outputFolder, "djmi"));
                }
                string djmiFilename = Path.Combine(outputFolder, "djmi", $"djmi{i}.png");
                try
                {
                    image.Save(djmiFilename, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"保存图片失败: {e.Message}");
                }
                djmiImages.Add(djmiImage);

            }
            //保存djmiImages为npy文件
            // 将列表转换为NumSharp数组
            //NDArray numpyArray = new NDArray(djmiImages.ToArray());
            byte[][,,] djmiArray = djmiImages.ToArray();
            // 获取数组的维度
            int dim1 = djmiArray.Length;//列表长度
            int dim2 = djmiArray[0].GetLength(0);
            int dim3 = djmiArray[0].GetLength(1);
            int dim4 = djmiArray[0].GetLength(2);
            // 创建一个NumSharp数组
            var npArray = new NDArray(typeof(byte), new NumSharp.Shape(dim1, dim2, dim3, dim4));
            // 将C#的byte[][,,]数组转换为NumSharp数组
            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    for (int k = 0; k < dim3; k++)
                    {
                        for (int l = 0; l < dim4; l++)
                        {
                            npArray[i, j, k, l] = djmiArray[i][j, k, l];
                        }
                    }
                }
            }
            if (!Directory.Exists(Path.Combine(outputFolder, "_npy")))
            {
                Directory.CreateDirectory(Path.Combine(outputFolder, "_npy"));
            }
            string outputFilename = Path.Combine(outputFolder, "_npy", "djmi.npy");
            //保存.npy文件
            try
            {
                np.save(outputFilename, npArray);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            return djmiImages;
        }
        #endregion
        #region generate_djmi

        static byte[,,] GenerateDjmiImage(double[,] displacement_data, int rgbScale)
        {
            // 将原始数组重新整形为三维数组 (100, 25, 3) 三个参数：帧数、骨架数、三维信息

            double[,,] reshapedData = Reshape(displacement_data, 100, 25, 3);
            int numFrames = reshapedData.GetLength(0);
            int numJoints = reshapedData.GetLength(1);
            int numDimensions = reshapedData.GetLength(2);
            int djmiHeight = numFrames;
            int djmiWidth = numJoints * 4;
            //Emgu.CV.Mat djmi = new Emgu.CV.Mat(djmiHeight, djmiWidth, DepthType.Cv8U, 3);
            byte[,,] djmi = new byte[djmiHeight, djmiWidth, 3];
            double[,] first = new double[numJoints, numDimensions];
            double[,] last = new double[numJoints, numDimensions];
            for (int j = 0; j < numJoints; j++)
            {
                for (int k = 0; k < numDimensions; k++)
                {
                    first[j, k] = reshapedData[0, j, k];
                }
            }
            //last = first;

            for (int j = 0; j < numJoints; j++)
            {
                for (int k = 0; k < numDimensions; k++)
                {
                    last[j, k] = first[j, k];
                }
            }
            for (int frameIndex = 0; frameIndex < numFrames; frameIndex++)
            {
                double[,] now = new double[numJoints, numDimensions];

                // 复制数据到 'now'
                for (int jointIndex = 0; jointIndex < numJoints; jointIndex++)
                {
                    for (int dimension = 0; dimension < numDimensions; dimension++)
                    {
                        now[jointIndex, dimension] = reshapedData[frameIndex, jointIndex, dimension];
                    }
                }

                for (int jointIndex = 0; jointIndex < numJoints; jointIndex++)
                {
                    //相对于第一帧位移录入
                    int pixelX = jointIndex + 25;

                    for (int dimension = 0; dimension < numDimensions; dimension++)
                    {
                        double displacement = now[jointIndex, dimension] - first[jointIndex, dimension];
                        if (displacement >= 0)
                        {
                            int newValue = (int)Math.Min(255, Math.Abs(displacement) * rgbScale);
                            djmi[frameIndex, jointIndex, dimension] = (byte)newValue;
                        }
                        else
                        {
                            int newValue = (int)Math.Min(255, Math.Abs(displacement) * rgbScale);
                            djmi[frameIndex, pixelX, dimension] = (byte)newValue;
                        }
                    }
                    //即时位移数据录入
                    pixelX += 25; // 将 pixelX 增加 25
                    double[] displacementData = new double[numDimensions]; // 创建一个与 numDimensions 大小相匹配的数组
                    for (int dimension = 0; dimension < numDimensions; dimension++)
                    {
                        displacementData[dimension] = now[jointIndex, dimension] - last[jointIndex, dimension];
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        int newValue = Math.Min(255, 127 + (int)(displacementData[i] * rgbScale));
                        djmi[frameIndex, pixelX, i] = (byte)newValue;
                    }
                    //即时位移方向录入
                    pixelX += 25;
                    double[] displacementOriention = (double[])displacementData.Clone();
                    //计算该数组的模长
                    double temp = displacementOriention.Select(x => x * x).Sum();
                    double arrayLength = Math.Sqrt(temp);//模长
                    double[] normalized_dispalcementOriention = new double[numDimensions];
                    if (displacementOriention.Length != 0)
                    {
                        for (int dimension = 0; dimension < numDimensions; dimension++)
                        {
                            normalized_dispalcementOriention[dimension] = displacementOriention[dimension] / arrayLength;
                        }
                    }
                    else
                    {//创建相同形状的数组，元素全部置0
                        normalized_dispalcementOriention = new double[displacementOriention.Length];
                    }
                    for (int dimension = 0; dimension < numDimensions; dimension++)
                    {
                        djmi[frameIndex, pixelX, dimension] = (byte)(128 + normalized_dispalcementOriention[dimension] * 128);
                    }
                    #region 已注销的方法
                    //double[] displacementSquared = new double[numDimensions];
                    //for (int dimension = 0; dimension < numDimensions; dimension++)
                    //{
                    //    displacementSquared[dimension] = Math.Pow(displacementArray[dimension], 2);
                    //}
                    //double[] displacementSqrt = new double[numDimensions];
                    //for (int dimension = 0; dimension < numDimensions; dimension++)
                    //{
                    //    int j = (dimension + 1) % numDimensions;
                    //    displacementSqrt[dimension] = Math.Sqrt(displacementSquared[dimension] + displacementSquared[j]);
                    //}
                    //// 更新 djmi 数组
                    //for (int dimension = 0; dimension < numDimensions; dimension++)
                    //{
                    //    if (displacementSqrt[dimension] > 0)//displacementSqrt[i]==0 或者>0
                    //    {
                    //        int newValue = Math.Min(255, Math.Max(0, (int)(now[jointIndex, dimension] / displacementSqrt[dimension])));
                    //        djmi[frameIndex, pixelX, dimension] = (byte)newValue;
                    //    }
                    //    else
                    //    {
                    //        djmi[frameIndex, pixelX, dimension] = 0;
                    //    }
                    //}
                    #endregion

                }
                //复制 now 数组到 last 数组
                for (int jointIndex = 0; jointIndex < numJoints; jointIndex++)
                {
                    for (int dimension = 0; dimension < numDimensions; dimension++)
                    {
                        last[jointIndex, dimension] = now[jointIndex, dimension];
                    }
                }
                //last = now;
            }
            ////保存djmi为为npy文件
            //if (!Directory.Exists(Path.Combine(outputFolder, "_npy")))
            //{
            //    Directory.CreateDirectory(Path.Combine(outputFolder, "_npy"));
            //}
            //string outputFilename = Path.Combine(outputFolder, "_npy", "djmi.npy");
            //try
            //{
            //    // 将byte[,,]数组转换为NumSharp数组
            //    NDArray numpyArray = new NDArray(djmi);
            //    // 保存.npy文件
            //    np.save(outputFilename, numpyArray);

            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine($"保存数组失败: {e.Message}");
            //}
            return djmi;
        }

        //将二维数组 100*75转化为三维数组 100*25*3
        static double[,,] Reshape(double[,] source, int totalFrames, int totalJoints, int totalDimensions)
        {
            double[,,] target = new double[totalFrames, totalJoints, totalDimensions];

            if (source.Length != totalFrames * totalJoints * totalDimensions)
            {
                throw new ArgumentException("Invalid source array size.");
            }


            for (int i = 0; i < totalFrames; i++)
            {
                int index = 0;
                for (int j = 0; j < totalJoints; j++)
                {
                    for (int k = 0; k < totalDimensions; k++)
                    {
                        target[i, j, k] = source[i, index];//j*k=index
                        index++;
                    }

                }
            }

            return target;
        }
        #endregion



        private void processPosData_Load_1(object sender, EventArgs e)
        {
          CenterForm.CenterFormOnScreen(this);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void selectFile_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "请选择文件夹";
                folderDialog.SelectedPath = Properties.Settings.Default.LastSelectedFolderPath;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolderPath = folderDialog.SelectedPath;
                    outputFolder = folderDialog.SelectedPath;//指定处理完数据存放的文件夹目录
                    string file = selectedFolderPath + "\\pos.txt";
                    if (File.Exists(file))
                    {
                        filePahBox.Text = file;
                    }
                    else
                    {
                        MessageBox.Show("指定的pos文件不存在");
                    }
                }
            }
        }

        private void browserFile_Click(object sender, EventArgs e)
        {
            // 指定文件的完整路径
            string filePath = filePahBox.Text;
            try
            {
                // 获取文件所在目录
                string directoryPath = System.IO.Path.GetDirectoryName(filePath);
                if (Directory.Exists(directoryPath))
                {
                    // 打开目录
                    OpenDirectory(directoryPath);
                }
                else
                {
                    MessageBox.Show("没有找到指定目录");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            
        }
        static void OpenDirectory(string path)
        {
            try
            {
                // 使用系统默认的文件资源管理器打开目录
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening directory: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FileDialog openFileDialog;
            openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog.Title = "Select a File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFilePath = openFileDialog.FileName;
                // 处理选定的文件路径...
                filePahBox.Text = selectedFilePath;
            }
        }
        private void customButton_Click(object sender, EventArgs e)
        {
            // 处理自定义按钮的逻辑...
            MessageBox.Show("Custom Button Clicked!");
        }
    }
}