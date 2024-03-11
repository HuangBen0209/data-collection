using HB.FormSettings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using 数据采集.Properties;

namespace 数据采集
{
    public partial class SampleShow : Form
    {
        ScreenAdaptationUtility sau;
        private int currentImageIndex = 1;
        private string imageFolderPath = Properties.Settings.Default.LastSelectedFolderPath; //上一次选中的文件夹路径
        private System.Threading.Timer imageTimer;
        private string[] imageFiles;
        private float Multiplyy = 1;//控制倍速
        public SampleShow()
        {
            InitializeComponent();
            ScreenAdaptationUtility.SetTag(this);
            sau = new ScreenAdaptationUtility(this);
            InitializeTimer();
            LoadImageFiles();
            SampleShow_Load(this, null);
            Multiply.SelectedItem = Multiplyy.ToString(); // 设置为倍数默认为1

        }
        private void SampleShow_Load(object sender, EventArgs e)
        {
            suspend.Enabled = false;
            replay.Enabled = false;
            //this.WindowState = FormWindowState.Maximized;  //初始化窗体最大化
            FilenameBox.Text = Properties.Settings.Default.LastSelectedFolderPath;
            HB.FormSetting.CenterFormOnScreen(this);
        }
        private void SampleShow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (suspend.Enabled == true)
                suspend_Click(this, null);//关闭窗口之前点几暂停按钮，防止继续访问图片，避免访问未加载的资源
            if (MessageBox.Show("确定要关闭窗体吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //关闭窗口
                // 停止定时器回调
                imageTimer.Change(Timeout.Infinite, Timeout.Infinite);
                // 等待回调完成，增加等待时间以确保所有回调都完成
                Thread.Sleep(1000);
                // 释放定时器
                imageTimer.Dispose();

                // 释放图片资源
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                }
            }
            else//
            {
                //取消窗体关闭
                e.Cancel = true;
            }
        }
        private void InitializeTimer()
        {
            imageTimer = new System.Threading.Timer(TimerCallback, null, 0, Timeout.Infinite); // 初始间隔设置为无限ms-->只回调一次
        }
        private void TimerCallback(object state)
        {
            if (imageFiles != null && imageFiles.Length > 0)
            {
                if (currentImageIndex >= imageFiles.Length)
                {
                    currentImageIndex = 1; // 从第一张图片重新开始
                }
                string imagePath = Path.Combine(imageFolderPath, currentImageIndex + ".jpg");
                if (File.Exists(imagePath))
                {
                    int targetWidth = pictureBox1.Width; // 根据需要修改宽度
                    int targetHeight = pictureBox1.Height; // 根据需要修改高度
                    try
                    {
                        using (System.Drawing.Image originalImage = System.Drawing.Image.FromFile(imagePath))
                        {
                            // 生成缩略图
                            System.Drawing.Image thumbnail = originalImage.GetThumbnailImage(targetWidth, targetHeight, null, IntPtr.Zero);

                            // 将缩略图显示在 PictureBox 上
                            this.Invoke((Action)(() =>
                            {
                                pictureBox1.Image = thumbnail;
                                fps.Text = currentImageIndex.ToString();
                            }));

                        }
                        currentImageIndex++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("加载图片时出错：" + ex.Message);
                    }
                }
            }
        }
        private void LoadImageFiles()
        {
            if (Directory.Exists(imageFolderPath))
            {
             
                    imageFiles = Directory.GetFiles(imageFolderPath, "*.jpg");
            }
            else
            {
                 
                MessageBox.Show("指定文件夹不存在。");
            }
        }
        private void Play_Click(object sender, EventArgs e)
        {
           
            if (imageFiles != null && imageFiles.Length > 0)
            {
                Play.Enabled = false;
                suspend.Enabled = true;
                replay.Enabled = true;
                suspend.Focus();
                imageFolderPath = FilenameBox.Text;
                this.Invoke((Action)(() =>
                {
                    int period = (int)(60 / Multiplyy);
                    imageTimer.Change(0, period); // 启动定时器，每隔period  ms切换图片
                }));
            }
            else
            {
                MessageBox.Show("指定文件夹中未找到图像文件。");
            }
            Settings.Default.LastSelectedFolderPath = FilenameBox.Text;//保存当前文件夹路径到程序设置中
            Settings.Default.Save();
        }
        private void suspend_Click(object sender, EventArgs e)
        {
            Play.Enabled = true;
            suspend.Enabled = false;
            Play.Focus();
            // 停止定时器
            imageTimer.Change(Timeout.Infinite, Timeout.Infinite); //延迟无限毫秒执行，无限毫秒之后回调一次
        }
        private void replay_Click(object sender, EventArgs e)
        {
            currentImageIndex = 1;
            Play_Click(sender, e);
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }

        private void fps_TextChanged(object sender, EventArgs e)
        {
        }
        //选择图片文件夹
        private void ChoosePath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                // 对话框的标题
                folderDialog.Description = "请选择文件夹";
                folderDialog.SelectedPath = Settings.Default.LastSelectedFolderPath;//导航到上次一次选择的文件路径
                //folderDialog.SelectedPath = @"D:\Desktop\科研实践\Coding\数据采集\数据采集\bin\x64\Debug\DataSamples"; // 替换为你的默认路径
                // 打开对话框并获取用户选择的文件夹路径
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolderPath = folderDialog.SelectedPath;
                    FilenameBox.Text = selectedFolderPath;
                }
               
            }
        }
        private void UpdateMultiply()
        {
            string selectedValue = Multiply.SelectedItem.ToString();
            if (float.TryParse(selectedValue, out float result))
            //它表示这个参数在方法调用之前不需要被赋初值，而在方法调用之后将包含方法返回的值。
            {
                Multiplyy = result;
            }
        }

        private void FilenameBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Multiple_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Combo_Multiply_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Multiply_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateMultiply();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Neck_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void SampleShow_Resize(object sender, EventArgs e)
        {
            if (sau != null)
            {
                sau.AdaptToScreenResolution();
            }
        }
    }
}
