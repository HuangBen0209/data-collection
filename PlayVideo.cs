using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace 数据采集
{
    public partial class PlayVideo : Form
    {
        private int currentImageIndex = 1; // 从第一张图片开始
        private string imageFolderPath;// 替换为你的图片文件夹路径
        private System.Threading.Timer quickTimer;
        private int triggerCount = 0;
        private float Multiply;

        private readonly float x;
        private readonly float y;
        public PlayVideo()
        {
            InitializeComponent();
            PlayVideo_Load(this, null);
            this.WindowState = FormWindowState.Maximized;
            //timer1.Interval = Multiply; // 1000/interval=帧率
            timer1.Tick += Timer1_Tick;


           
                #region   初始化控件缩放

                x = Width;
                y = Height;
                setTag(this);

                #endregion

        }
        private void InitializeTimer()
        {
            // 创建一个 System.Threading.Timer，将触发间隔设置为5毫秒
            quickTimer = new System.Threading.Timer(TimerCallback, null, 0, 1);
        }
        private void TimerCallback(object state)
        {
            // 这里放置定时触发时要执行的代码
            triggerCount++;
            if (textBox2.InvokeRequired)
            {
                // 使用Invoke方法在UI线程上执行操作
                textBox2.Invoke(new MethodInvoker(() =>
                {
                    // 在这里执行需要访问textBox2的操作
                    textBox2.Text = triggerCount.ToString();
                }));
            }
            else
            {
                // 当在UI线程上时，可以直接操作控件
                textBox2.Text = "新的文本内容";
            }
         
            // 更新界面或执行其他操作
            // ...

            //// 判断是否达到指定触发次数，如果是，停止定时器
            //if (triggerCount >= 200)
            //{
            //    quickTimer.Dispose(); // 停止定时器
            //    // 执行完后的操作，如释放资源等
            //    // ...
            //}
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            currentImageIndex++;
            DisplayCurrentImage();
        }

        private void DisplayCurrentImage()
        {
            string imagePath = Path.Combine(imageFolderPath, currentImageIndex + ".bmp");
            if (File.Exists(imagePath))
            {
                //// 加载原始图片
                //Image originalImage = Image.FromFile(imagePath);
                //// 指定目标大小
                //int targetWidth = 800; // 你可以根据需要修改宽度
                //int targetHeight = 600; // 你可以根据需要修改高度
                //// 创建一个新的Bitmap，将原始图片调整为目标大小
                //Bitmap resizedImage = new Bitmap(originalImage, targetWidth, targetHeight);
                //// 将调整后的图片显示在pictureBox1中
                //pictureBox1.Image = resizedImage;
                //textBox1.Text = currentImageIndex.ToString();


                int targetWidth = 800; // 你可以根据需要修改宽度
                int targetHeight = 600; // 你可以根据需要修改高度
                // 创建一个新的Bitmap，将原始图片调整为目标大小
                Bitmap bm =new Bitmap(imagePath);
                Bitmap resized_bm = new Bitmap(bm, targetWidth, targetHeight);
                pictureBox1.Image = resized_bm;
                textBox1.Text = currentImageIndex.ToString();
            }
            else
            {
                // 当图片不存在时，停止定时器或采取其他操作
                timer1.Stop();
            }
        }
        #region 控件大小随窗体大小等比例缩放
        private void setTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                con.Tag = con.Width + ";" + con.Height + ";" + con.Left + ";" + con.Top + ";" + con.Font.Size;
                if (con.Controls.Count > 0) setTag(con);
            }
        }
        private void setControls(float newx, float newy, Control cons)
        {
            //遍历窗体中的控件，重新设置控件的值
            foreach (Control con in cons.Controls)
                //获取控件的Tag属性值，并分割后存储字符串数组
                if (con.Tag != null)
                {
                    var mytag = con.Tag.ToString().Split(';');
                    //根据窗体缩放的比例确定控件的值
                    con.Width = Convert.ToInt32(Convert.ToSingle(mytag[0]) * newx); //宽度
                    con.Height = Convert.ToInt32(Convert.ToSingle(mytag[1]) * newy); //高度
                    con.Left = Convert.ToInt32(Convert.ToSingle(mytag[2]) * newx); //左边距
                    con.Top = Convert.ToInt32(Convert.ToSingle(mytag[3]) * newy); //顶边距
                    var currentSize = Convert.ToSingle(mytag[4]) * newy; //字体大小                   
                    if (currentSize > 0) con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                    con.Focus();
                    if (con.Controls.Count > 0) setControls(newx, newy, con);
                }
        }

        /// <summary>
        /// 重置窗体布局
        /// </summary>
        private void ReWinformLayout()
        {
            var newx = Width / x;
            var newy = Height / y;
            setControls(newx, newy, this);

        }

        #endregion
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            
        }
        private void PlayVideo_Load(object sender, EventArgs e)
        {

            FileNameBox.Text = Properties.Settings.Default.LastSelectedFolderPath;

            Multiple.SelectedItem = Properties.Settings.Default.Multiple;
            UpdateMultiply();


            this.Resize += PlayVideo_Resize;
        }

        private void PlayVideo_Resize(object sender, EventArgs e)
        {
            ReWinformLayout();
        }

        private void UpdateMultiply()
        {
            string selectedValue = Multiple.SelectedItem.ToString();
            if (float.TryParse(selectedValue, out float result))
            {
                Multiply = result;
                timer1.Interval = (int)(30 / Multiply); // 设置计时器间隔
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            imageFolderPath = FileNameBox.Text;
            //Multiply=int.Parse(Multiple.Text);

            currentImageIndex = int.Parse(textBox1.Text);
            //MessageBox.Show(Multiply +"::"+ Multiple.Text);

            MessageBox.Show("倍数:"+Multiple.Text);
            if (Directory.Exists(imageFolderPath))
            {
                DisplayCurrentImage();
                timer1.Start();
            }
            else
            {
                MessageBox.Show("图片文件夹不存在！");
            }
            Properties.Settings.Default.LastSelectedFolderPath = FileNameBox.Text;
            Properties.Settings.Default.Multiple = Multiple.Text;
            Properties.Settings.Default.Save();

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "1";
            button1_Click(sender, e);

        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Start();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                // 设置对话框的标题
                folderDialog.Description = "请选择文件夹";
                folderDialog.SelectedPath = Properties.Settings.Default.LastSelectedFolderPath;
                //folderDialog.SelectedPath = @"D:\Desktop\科研实践\Coding\数据采集\数据采集\bin\x64\Debug\DataSamples"; // 替换为你的默认路径
                // 打开对话框并获取用户选择的文件夹路径
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolderPath = folderDialog.SelectedPath;
                    FileNameBox.Text = selectedFolderPath;
                }
            }


        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
         
        }

    

        private void Multiple_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateMultiply();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            InitializeTimer();
        }

        private void PlayVideo_Load_1(object sender, EventArgs e)
        {

        }
    }
}
