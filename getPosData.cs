using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Collections;
using System.Data.SqlClient;

using HBUtils.FormSet;

namespace 数据采集
{
    public partial class getPosData : Form
    {
        AutoAdaptForm sau;
        public getPosData()
        {
            InitializeComponent();
            AutoAdaptForm.SetTag(this);
            sau = new AutoAdaptForm(this);

        }

        KinectSensor sensor;
        int width, height, frames = 0;
        string currentRBGPath = ""; //数据存放位置的文件目录
        string excludeJoints = "ThumbLeft,ThumbRight";//排除这两个关节点，后面描述骨骼点用的到
        ArrayList allImg = new ArrayList(); //动态数组存放rgb图
        ArrayList allImg_ske = new ArrayList();//存放骨骼rgb图
        ArrayList allPos = new ArrayList(); // 用于存放追踪到的身体帧，便于后期一次性插入数据库

        private void sittingPostureDetect_Load(object sender, EventArgs e)
        {
            //CenterForm.CenterFormOnScreen(this);
            //this.WindowState = FormWindowState.Maximized;
            startButton.Enabled = false;
            sensor = KinectSensor.GetDefault();//获取当前摄像头
            if (sensor != null)
            {
                sensor.Open();
                MessageBox.Show("摄像头开启");
                ColorFrameSource cfs = sensor.ColorFrameSource;//获取颜色帧
                FrameDescription fdp = cfs.FrameDescription;//帧的描述，包含帧的相关各种参数
                width = fdp.Width;
                height = fdp.Height;
                MultiSourceFrameReader mfr = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Body);
                //订阅多源帧
                mfr.MultiSourceFrameArrived += mfr_MultiSourceFrameArrived;
            }
            else
            {
                MessageBox.Show("未检测到摄像头");
            }
        }
        private void mfr_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame msf = e.FrameReference.AcquireFrame();
            using (ColorFrame cf = msf.ColorFrameReference.AcquireFrame())
            {
                using (DepthFrame df = msf.DepthFrameReference.AcquireFrame())
                {
                    using (BodyFrame bf = msf.BodyFrameReference.AcquireFrame())
                    {
                        if (bf != null && cf != null && df != null)
                        {
                            if (currentRBGPath != "")
                            {
                                frames++;
                            }
                            #region 获取彩色图
                            //1将获得的彩色帧数组存入字节数组中
                            //2将字节加载到imag图中
                            //3将image图转化为bitmap图

                            //创建一个image图像,字节类型，采用rgba通道
                            Image<Bgra, byte> cImg = new Image<Bgra, byte>(cf.FrameDescription.Width, cf.FrameDescription.Height);
                            //Image<Bgra, byte> cImg = new Image<Bgra, byte>(1920, 1080);
                            ///创建一个对应大小的字节数组
                            byte[] pixels = new byte[cImg.Bytes.Count<byte>()];
                            //将彩色帧的数据转换并存入像素数组中
                            cf.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);//后面的参数是转换格式，将像素数据转换为bgra
                            cImg.Bytes = pixels;//将像素数据加载到cImg中
                            Bitmap bitmap_color = cImg.ToBitmap();//将图像转换为bitmap类型
                            #endregion
                            #region 获取深度图
                            //1将深度帧的像素存入数据
                            //2将数组中的像素写入位图
                            //3将位图添加到位图编码器中
                            //4将位图编码器写入内存流
                            //5用bitmap读取内存的字节数据流
                            int newDepthWidth = img_Depth.Width;
                            int newDepthHeight = img_Depth.Height;
                            newDepthWidth = df.FrameDescription.Width;
                            newDepthHeight = df.FrameDescription.Height;
                            //ushort[] depthPixelData = new ushort[newDepthWidth * newDepthHeight];
                            ushort[] depthPixelData = new ushort[df.FrameDescription.LengthInPixels];
                            df.CopyFrameDataToArray(depthPixelData);//将深度帧复制到数组中
                            //创建了可写入的位图，用于存储深度图像的可视化表示。它的参数指定了位图的宽度、高度、分辨率、像素格式等。
                            WriteableBitmap wab//width=512,height=414   
                               = new WriteableBitmap(newDepthWidth,//dpi表示一英寸有多少个像素点，一行一列各有多少个
                                   newDepthHeight, 96, 96, PixelFormats.Bgr565, null);//PixelFormats.Bgr565每个像素由16位数据组成，565代表blue占用5位数据
                            //创建 Int32Rect 对象，表示位图的矩形区域，覆盖整个深度图像。
                            System.Windows.Int32Rect depthBitmapRect = new System.Windows.Int32Rect(0, 0, df.FrameDescription.Width, df.FrameDescription.Height);// 创建一个矩形，从(0,0)开始，长宽为深度图的长宽
                            //写入像素，.width*2表示一个像素用两个字节16位，对应上面的bgr565,，这里用width不用height是因为width代表了一行像素的个数，这个参数表示，当写满这么多自动切换到下一行，继续写入像素
                            wab.WritePixels(depthBitmapRect, depthPixelData, df.FrameDescription.Width * 2, 0, 0);//0表示从数组的第一位开始写入
                            BitmapEncoder encoder = new BmpBitmapEncoder();
                            /*
                             * encoder.Frames表示帧集合，可用add增加帧，
                             * bitmapframe.create(wab) 表示用上面wab存取好的像素，创建一个位图帧，存入Frames中
                            */
                            encoder.Frames.Add(BitmapFrame.Create(wab));
                            MemoryStream ms = new MemoryStream();//开启一个内存流，存放编码后的图像格式
                            encoder.Save(ms); //将编码好的格式放入内存流中
                            Bitmap bitmap_depth = new Bitmap(ms); //用bitmap对象显示图像数据
                            ms.Close();
                            ms.Dispose();
                            #endregion
                            #region 显示图片至img框
                            img_RGB.Image = bitmap_color;
                            if (currentRBGPath != "")//准备开始时创建对应文件夹，存入数据
                            {
                                allImg.Add(ResizeImage(bitmap_color, 660, 450)); //将每一帧图片加入到动态数组中
                                //bitmap_color.Save(currentRBGPath + "\\" + frames + ".bmp");
                            }
                            img_Depth.Image = bitmap_depth;
                            #endregion
                            #region 获取骨骼数据
                            Body[] bodies = new Body[bf.BodyCount];
                            bf.GetAndRefreshBodyData(bodies);
                            foreach (Body bd in bodies)
                            {
                                if (bd.IsTracked)
                                {
                                    IReadOnlyDictionary<JointType, Joint> joints = bd.Joints;//将追踪到的身体数据，把type和position用键值对的形式存放到可读字典中
                                    if (currentRBGPath != "")
                                    {
                                        allPos.Add(bd);  //将追中到的body存入alPos动态数组中
                                    }
                                    #region 关节点
                                    Joint jHead = joints[JointType.Head];
                                    Joint jNeck = joints[JointType.Neck];
                                    Joint jSpineShoulder = joints[JointType.SpineShoulder];
                                    Joint jSpineMid = joints[JointType.SpineMid];
                                    Joint jSpineBase = joints[JointType.SpineBase];

                                    Joint jShoulderLeft = joints[JointType.ShoulderLeft];
                                    Joint jElbowLeft = joints[JointType.ElbowLeft];
                                    Joint jWristLeft = joints[JointType.WristLeft];
                                    Joint jHandLeft = joints[JointType.HandLeft];
                                    Joint jHandTipLeft = joints[JointType.HandTipLeft];
                                    Joint jHipLeft = joints[JointType.HipLeft];
                                    Joint jKneeLeft = joints[JointType.KneeLeft];
                                    Joint jAnkleLeft = joints[JointType.AnkleLeft];
                                    Joint jFootLeft = joints[JointType.FootLeft];

                                    Joint jShoulderRight = joints[JointType.ShoulderRight];
                                    Joint jElbowRight = joints[JointType.ElbowRight];
                                    Joint jWristRight = joints[JointType.WristRight];
                                    Joint jHandRight = joints[JointType.HandRight];
                                    Joint jHandTipRight = joints[JointType.HandTipRight];
                                    Joint jHipRight = joints[JointType.HipRight];
                                    Joint jKneeRight = joints[JointType.KneeRight];
                                    Joint jAnkleRight = joints[JointType.AnkleRight];
                                    Joint jFootRight = joints[JointType.FootRight];
                                    #endregion

                                    #region 描绘彩色图骨骼线
                                    System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.Red, 30);//
                                    using (Graphics g = Graphics.FromImage(bitmap_color))
                                    {
                                        if (notEmpty(jHead, jNeck))
                                            g.DrawLine(p, coordinateColor_change(jHead), coordinateColor_change(jNeck));

                                        if (notEmpty(jNeck, jSpineShoulder))
                                            g.DrawLine(p, coordinateColor_change(jNeck), coordinateColor_change(jSpineShoulder));

                                        if (notEmpty(jSpineShoulder, jSpineMid))
                                            g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jSpineMid));

                                        if (notEmpty(jSpineMid, jSpineBase))
                                            g.DrawLine(p, coordinateColor_change(jSpineMid), coordinateColor_change(jSpineBase));

                                        #region 左半身
                                        if (notEmpty(jSpineShoulder, jShoulderLeft))
                                            g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jShoulderLeft));

                                        if (notEmpty(jShoulderLeft, jElbowLeft))
                                            g.DrawLine(p, coordinateColor_change(jShoulderLeft), coordinateColor_change(jElbowLeft));

                                        if (notEmpty(jElbowLeft, jWristLeft))
                                            g.DrawLine(p, coordinateColor_change(jElbowLeft), coordinateColor_change(jWristLeft));

                                        if (notEmpty(jWristLeft, jHandLeft))
                                            g.DrawLine(p, coordinateColor_change(jWristLeft), coordinateColor_change(jHandLeft));

                                        if (notEmpty(jHandLeft, jHandTipLeft))
                                            g.DrawLine(p, coordinateColor_change(jHandLeft), coordinateColor_change(jHandTipLeft));

                                        if (notEmpty(jSpineBase, jHipLeft))
                                            g.DrawLine(p, coordinateColor_change(jSpineBase), coordinateColor_change(jHipLeft));

                                        if (notEmpty(jHipLeft, jKneeLeft))
                                            g.DrawLine(p, coordinateColor_change(jHipLeft), coordinateColor_change(jKneeLeft));

                                        if (notEmpty(jKneeLeft, jAnkleLeft))
                                            g.DrawLine(p, coordinateColor_change(jKneeLeft), coordinateColor_change(jAnkleLeft));

                                        if (notEmpty(jAnkleLeft, jFootLeft))
                                            g.DrawLine(p, coordinateColor_change(jAnkleLeft), coordinateColor_change(jFootLeft));
                                        #endregion

                                        #region 右半身
                                        if (notEmpty(jSpineShoulder, jShoulderRight))
                                            g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jShoulderRight));

                                        if (notEmpty(jShoulderRight, jElbowRight))
                                            g.DrawLine(p, coordinateColor_change(jShoulderRight), coordinateColor_change(jElbowRight));

                                        if (notEmpty(jElbowRight, jWristRight))
                                            g.DrawLine(p, coordinateColor_change(jElbowRight), coordinateColor_change(jWristRight));

                                        if (notEmpty(jWristRight, jHandRight))
                                            g.DrawLine(p, coordinateColor_change(jWristRight), coordinateColor_change(jHandRight));

                                        if (notEmpty(jHandRight, jHandTipRight))
                                            g.DrawLine(p, coordinateColor_change(jHandRight), coordinateColor_change(jHandTipRight));

                                        if (notEmpty(jSpineBase, jHipRight))
                                            g.DrawLine(p, coordinateColor_change(jSpineBase), coordinateColor_change(jHipRight));

                                        if (notEmpty(jHipRight, jKneeRight))
                                            g.DrawLine(p, coordinateColor_change(jHipRight), coordinateColor_change(jKneeRight));

                                        if (notEmpty(jKneeRight, jAnkleRight))
                                            g.DrawLine(p, coordinateColor_change(jKneeRight), coordinateColor_change(jAnkleRight));

                                        if (notEmpty(jAnkleRight, jFootRight))
                                            g.DrawLine(p, coordinateColor_change(jAnkleRight), coordinateColor_change(jFootRight));

                                        #endregion
                                        #region 画关节坐标点
                                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; //AntiAlias反锯齿画法
                                        System.Drawing.Brush bush = new SolidBrush(System.Drawing.Color.Yellow);//填充的颜色

                                        foreach (var j in joints)
                                        {

                                            if (excludeJoints.IndexOf(j.Key.ToString()) == -1)//在排除的关节点里面索引所有的节点，如果没有找到，则返回-1
                                            {
                                                //用圆形画笔画出骨骼点 像素为20，圆心向坐左上角偏移
                                                g.FillEllipse(bush, coordinateColor_change(joints[j.Key]).X - 5, coordinateColor_change(joints[j.Key]).Y - 5, 30, 30);
                                            }
                                        }

                                        #endregion
                                    }

                                    #endregion

                                    #region 描绘深度图骨骼线
                                    p = new System.Drawing.Pen(System.Drawing.Color.Red, 12);
                                    using (Graphics g = Graphics.FromImage(bitmap_depth))
                                    {
                                        g.DrawLine(p, coordinateDepth_change(jHead), coordinateDepth_change(jNeck));
                                        g.DrawLine(p, coordinateDepth_change(jNeck), coordinateDepth_change(jSpineShoulder));
                                        g.DrawLine(p, coordinateDepth_change(jSpineShoulder), coordinateDepth_change(jSpineMid));
                                        g.DrawLine(p, coordinateDepth_change(jSpineMid), coordinateDepth_change(jSpineBase));

                                        #region 左半身                     
                                        g.DrawLine(p, coordinateDepth_change(jSpineShoulder), coordinateDepth_change(jShoulderLeft));
                                        g.DrawLine(p, coordinateDepth_change(jShoulderLeft), coordinateDepth_change(jElbowLeft));
                                        g.DrawLine(p, coordinateDepth_change(jElbowLeft), coordinateDepth_change(jWristLeft));
                                        g.DrawLine(p, coordinateDepth_change(jWristLeft), coordinateDepth_change(jHandLeft));
                                        g.DrawLine(p, coordinateDepth_change(jHandLeft), coordinateDepth_change(jHandTipLeft));
                                        g.DrawLine(p, coordinateDepth_change(jSpineBase), coordinateDepth_change(jHipLeft));
                                        g.DrawLine(p, coordinateDepth_change(jHipLeft), coordinateDepth_change(jKneeLeft));
                                        g.DrawLine(p, coordinateDepth_change(jKneeLeft), coordinateDepth_change(jAnkleLeft));
                                        g.DrawLine(p, coordinateDepth_change(jAnkleLeft), coordinateDepth_change(jFootLeft));
                                        #endregion

                                        #region 右半身                     
                                        g.DrawLine(p, coordinateDepth_change(jSpineShoulder), coordinateDepth_change(jShoulderRight));
                                        g.DrawLine(p, coordinateDepth_change(jShoulderRight), coordinateDepth_change(jElbowRight));
                                        g.DrawLine(p, coordinateDepth_change(jElbowRight), coordinateDepth_change(jWristRight));
                                        g.DrawLine(p, coordinateDepth_change(jWristRight), coordinateDepth_change(jHandRight));
                                        g.DrawLine(p, coordinateDepth_change(jHandRight), coordinateDepth_change(jHandTipRight));
                                        g.DrawLine(p, coordinateDepth_change(jSpineBase), coordinateDepth_change(jHipRight));
                                        g.DrawLine(p, coordinateDepth_change(jHipRight), coordinateDepth_change(jKneeRight));
                                        g.DrawLine(p, coordinateDepth_change(jKneeRight), coordinateDepth_change(jAnkleRight));
                                        g.DrawLine(p, coordinateDepth_change(jAnkleRight), coordinateDepth_change(jFootRight));
                                        #endregion

                                        #region 画关节坐标点
                                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; //AntiAlias反锯齿画法
                                        System.Drawing.Brush bush = new SolidBrush(System.Drawing.Color.Yellow);//填充的颜色

                                        foreach (var j in joints)
                                        {

                                            if (excludeJoints.IndexOf(j.Key.ToString()) == -1)//在排除的关节点里面索引所有的节点，如果没有找到，则返回-1
                                            {
                                                //用圆形画笔画出骨骼点 像素为10，圆心向坐左上角偏移
                                                g.FillEllipse(bush, coordinateDepth_change(joints[j.Key]).X - 5, coordinateDepth_change(joints[j.Key]).Y - 5, 10, 10);
                                            }
                                        }
                                        #endregion
                                    }
                                    #endregion

                                    img_RGB.Image = bitmap_color;
                                    img_Depth.Image = bitmap_depth;

                                    #region 关节点坐标 这个会影响RGB图片展示---注销
                                    Head.Text = "Head：" + jHead.Position.X + "," + jHead.Position.Y + "," + jHead.Position.Z;
                                    Neck.Text = "Neck：" + jNeck.Position.X + "," + jNeck.Position.Y + "," + jNeck.Position.Z;
                                    SpineShoulder.Text = "SpineShoulder：" + jSpineShoulder.Position.X + "," + jSpineShoulder.Position.Y + "," + jSpineShoulder.Position.Z;
                                    SpineMid.Text = "SpineMid：" + jSpineMid.Position.X + "," + jSpineMid.Position.Y + "," + jSpineMid.Position.Z;
                                    SpineBase.Text = "SpineBase：" + jSpineBase.Position.X + "," + jSpineBase.Position.Y + "," + jSpineBase.Position.Z;

                                    ShoulderLeft.Text = "ShoulderLeft：" + jShoulderLeft.Position.X + "," + jShoulderLeft.Position.Y + "," + jShoulderLeft.Position.Z;
                                    ElbowLeft.Text = "ElbowLeft：" + jElbowLeft.Position.X + "," + jElbowLeft.Position.Y + "," + jElbowLeft.Position.Z;
                                    WristLeft.Text = "WristLeft：" + jWristLeft.Position.X + "," + jWristLeft.Position.Y + "," + jWristLeft.Position.Z;
                                    HandLeft.Text = "HandLeft：" + jHandLeft.Position.X + "," + jHandLeft.Position.Y + "," + jHandLeft.Position.Z;
                                    HandTipLeft.Text = "HandTipLeft：" + jHandTipLeft.Position.X + "," + jHandTipLeft.Position.Y + "," + jHandTipLeft.Position.Z;
                                    HipLeft.Text = "HipLeft：" + jHipLeft.Position.X + "," + jHipLeft.Position.Y + "," + jHipLeft.Position.Z;
                                    KneeLeft.Text = "KneeLeft：" + jKneeLeft.Position.X + "," + jKneeLeft.Position.Y + "," + jKneeLeft.Position.Z;
                                    AnkleLeft.Text = "AnkleLeft：" + jAnkleLeft.Position.X + "," + jAnkleLeft.Position.Y + "," + jAnkleLeft.Position.Z;
                                    FootLeft.Text = "FootLeft：" + jFootLeft.Position.X + "," + jFootLeft.Position.Y + "," + jFootLeft.Position.Z;

                                    ShoulderRight.Text = "ShoulderRight：" + jShoulderRight.Position.X + "," + jShoulderRight.Position.Y + "," + jShoulderRight.Position.Z;
                                    ElbowRight.Text = "ElbowRight：" + jElbowRight.Position.X + "," + jElbowRight.Position.Y + "," + jElbowRight.Position.Z;
                                    WristRight.Text = "WristRight：" + jWristRight.Position.X + "," + jWristRight.Position.Y + "," + jWristRight.Position.Z;
                                    HandRight.Text = "HandRight：" + jHandRight.Position.X + "," + jHandRight.Position.Y + "," + jHandRight.Position.Z;
                                    HandTipRight.Text = "HandTipRight：" + jHandTipRight.Position.X + "," + jHandTipRight.Position.Y + "," + jHandTipRight.Position.Z;
                                    HipRight.Text = "HipRight：" + jHipRight.Position.X + "," + jHipRight.Position.Y + "," + jHipRight.Position.Z;
                                    KneeRight.Text = "KneeRight：" + jKneeRight.Position.X + "," + jKneeRight.Position.Y + "," + jKneeRight.Position.Z;
                                    AnkleRight.Text = "AnkleRight：" + jAnkleRight.Position.X + "," + jAnkleRight.Position.Y + "," + jAnkleRight.Position.Z;
                                    FootRight.Text = "FootRight：" + jFootRight.Position.X + "," + jFootRight.Position.Y + "," + jFootRight.Position.Z;
                                #endregion
                            }
                        }
                            #endregion
                            if (currentRBGPath != "")
                            {
                                allImg_ske.Add(bitmap_color); //将每一帧图片加入到动态数组中
                            }
                        }
                    }
                }
            }
        }
        private bool notEmpty(Joint joint1, Joint joint2)
        {
            if ((coordinateColor_change(joint1).X != 0 && coordinateColor_change(joint1).Y != 0) &&
                (coordinateColor_change(joint2).X != 0 && coordinateColor_change(joint2).Y != 0))
                return true;
            else
                return false;
        }
        //彩色帧坐标二维转换
        private Point coordinateColor_change(Joint jPoint)
        {//转换成二维坐标 将3D摄像头空间的点映射到彩色图像的坐标
            ColorSpacePoint colorPoint = this.sensor.CoordinateMapper.MapCameraPointToColorSpace(jPoint.Position);
            Rectangle rc = new Rectangle(0, 0, 1920, 1080);//一个矩形：左上角坐标 width,height
            Point cP = new Point((int)colorPoint.X, (int)colorPoint.Y);
            //如果像素点超过了矩形的大小，则将他放在1920,1080位置处
            if (!rc.Contains(cP))
            {
                cP = new Point(0, 0);
            }
            return cP;
        }
        //深度帧坐标二维转换
        private Point coordinateDepth_change(Joint jPoint)
        {//转换成二维坐标
            DepthSpacePoint depthPoint = this.sensor.CoordinateMapper.MapCameraPointToDepthSpace(jPoint.Position);
            Rectangle rc = new Rectangle(0, 0, 600, 480);
            Point dP = new Point((int)depthPoint.X, (int)depthPoint.Y);
            if (!rc.Contains(dP))
            {
                dP = new Point(0, 0);
            }
            return dP;
        }
        //重新设置图像大小
        private static Bitmap ResizeImage(Bitmap bmp, int newWidth, int newHeight)
        {
            try
            {

                Bitmap b = new Bitmap(newWidth, newHeight);
                Graphics g = Graphics.FromImage(b);//在对象b上面创建一个画笔
                //用像素点作为单位 将bmp的矩形区域写入 新的位图中
                g.DrawImage(bmp, new Rectangle(0, 0, newWidth, newHeight), new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
                g.Dispose();
                return b;
            }
            catch { return null; }
        }
        //保存信息函数
        public void RecordInfo(string filePath, string msg)
        {
            if (!File.Exists(filePath))
            {
                FileStream fs = File.Create(filePath);
                fs.Close();
                fs.Dispose();
            }
            StreamWriter sw = File.AppendText(filePath);//用追加文本的方式打开filePath这个文件
            sw.WriteLine(msg);
            sw.Close();
            sw.Dispose();
        }
        //保存数据
        private void button1_Click(object sender, EventArgs e)
        {
            DirectoryInfo diFilePath = new DirectoryInfo(currentRBGPath);//DirectoryInfo 操作目录的类
            diFilePath = diFilePath.Parent;  //acttype//actor这个文件夹
            string posFilePath = diFilePath.FullName.ToString() + "\\pos.txt";//构建完整的文件路径
            frames = 0;
            currentRBGPath = "";
            startButton.Enabled = false;//防止重复点击该按钮
            #region 记录坐标数据
            #region 通过sql构建datatable结构
            DataTable dt = new DataTable();
            string sqlstr = "select * from Table_1 where 1=2";//不查询任何数据，只查询表的结构
            dt = King.DataBase.SqlServer.GetDataSet(sqlstr).Tables[0];//获取Table_1表的结构
            #endregion
            int frameorder = 0;
            foreach (object o in allPos)
            {
                frameorder++;//body对象数
                Microsoft.Kinect.Body bd = o as Microsoft.Kinect.Body;//将o转化为mkb对象

                string recordS = frameorder + "_";

                #region 构建关节坐标点，写入文件
                foreach (var j in bd.Joints)
                {
                    string jointName = j.Key.ToString();
                    string jointPosition = j.Value.Position.X + "," + j.Value.Position.Y + "," + j.Value.Position.Z;
                    recordS += jointName + ":" + jointPosition + "|";
                }
                RecordInfo(posFilePath, recordS);
                #endregion

                #region 构建datatable数据
                DataRow dr = dt.NewRow();
                dr["TableKey"] = Guid.NewGuid().ToString();//生成一个全局Uid
                dr["VideoType"] = actType.Text; //动作类型
                dr["FileName"] = new FileInfo(posFilePath).Directory.Name.ToString();//存放的是actType//actor的路径
                dr["FrameOrder"] = frameorder;
                dr["Actor"] = actor.Text;
                foreach (var j in bd.Joints)
                {
                    dr[j.Key.ToString()] = j.Value.Position.X + "," + j.Value.Position.Y + "," + j.Value.Position.Z;
                }

                dt.Rows.Add(dr);

                #endregion
            }

            #region 批量插入
            SqlConnection sqlConn = new SqlConnection(King.StringOper.getConfigValue("sqlconnstring"));
            SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConn);

            bulkCopy.DestinationTableName = "Table_1";//写入数据库哪个表格中
            bulkCopy.BatchSize = dt.Rows.Count;// 一次性插入多少行的数据，减少数据库的插入次数

            try
            {
                sqlConn.Open();
                if (dt != null && dt.Rows.Count != 0)
                    bulkCopy.WriteToServer(dt);
                //MessageBox.Show("插入数据完成");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                sqlConn.Close();
                if (bulkCopy != null)
                    bulkCopy.Close();
            }
            #endregion
            #endregion

            #region 保存图片数据  实时检测时注释掉这段代码
            int imgOrder = 0;
            int imgOrder2 = 0;
            //#region 保存RGB图像
            //foreach (object o in allImg)
            //{
            //    imgOrder++;
            //    string imgPath = diFilePath.FullName + "\\RGB\\" + imgOrder.ToString() + ".jpg";
            //    (o as Bitmap).Save(imgPath);
            //}
            //#endregion 
            foreach (object o in allImg_ske)
            {
                imgOrder2++;
                string imgPath_ske = diFilePath.FullName + "\\RGB_Ske\\" + imgOrder2.ToString() + ".jpg";
                (o as Bitmap).Save(imgPath_ske);
            }
            #endregion

            preButton.Enabled = true;
            delButton.Enabled = true;

            allImg.Clear();
            allImg_ske.Clear();
            allPos.Clear();

            MessageBox.Show("保存完毕。");
        }
        //准备保存数据
        private void button2_Click(object sender, EventArgs e)
        {
            if (actor.Text == "" || (actType.Text == ""))
            {
                MessageBox.Show("动作类型或者动作人不能为空");
                return;
            }
            delButton.Enabled = true;
            #region 创建文件保存路径
            //在程序启动的目录下创建一个Datas的文件夹,里面有两个目录，DataSample存放样本数据，也就是该cs采集的数据
            string rootFolder = AppDomain.CurrentDomain.BaseDirectory;
            string[] spiltFolder = rootFolder.Split(new string[] { "数据采集" }, StringSplitOptions.None);
            string pathStr = spiltFolder[0] + "\\Datas" + "\\DataSamples" + "\\";
            //用DirectoryInfo类操作系统文件(这个类是实例化对象，功能强大一点，Directory是静态的，功能差不多）
            //创建一个记录动作类型的文件夹
            DirectoryInfo dir = new DirectoryInfo(pathStr);
            if (!dir.Exists)
            {
                dir.Create();
            }
            dir = new DirectoryInfo(pathStr + actType.Text);//实例化一个对象指向该文件夹
            if (!dir.Exists)
            {
                dir.Create();
            }
            //创建动作人对应的文件夹
            dir = new DirectoryInfo(pathStr + actType.Text + "\\" + actor.Text);
            if (!dir.Exists)
            {
                dir.Create();
            }
            string text = @"" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day;
            string videoDir = text + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second;
            //创立时间相关的文件夹
            dir = new DirectoryInfo(pathStr + actType.Text + "\\" + actor.Text + "\\" + videoDir + "\\RGB");
            if (!dir.Exists)
            {
                dir.Create();
            }
            dir = new DirectoryInfo(pathStr + actType.Text + "\\" + actor.Text + "\\" + videoDir + "\\RGB_Ske");
            if (!dir.Exists)
            {
                dir.Create();
            }

            dirNameBox.Text = dir.FullName.ToString();  //将文件夹完整路径：（currentRGB的文件夹(具体时间）显示到textbox
            #endregion
            currentRBGPath = dir.ToString();

            preButton.Enabled = false;
            startButton.Enabled = true;

        }
        //删除数据
        private void button3_Click(object sender, EventArgs e)
        {
            preButton.Enabled = true;
            delButton.Enabled = false;
            DialogResult dr = MessageBox.Show("删除数据", "确定删除当前数据吗？", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (dr.Equals(DialogResult.OK))
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(dirNameBox.Text);
                    if (!dir.Exists)
                    {
                        return;
                    }
                    string sqlstr = "delete from Table_1 where FileName=@FileName";
                    using (SqlConnection sqlConn = new SqlConnection(King.StringOper.getConfigValue("sqlconnstring")))
                    {
                        sqlConn.Open();
                        using (SqlCommand cmd = new SqlCommand(sqlstr, sqlConn))
                        {
                            cmd.Parameters.AddWithValue("@FileName", dir.Parent.Name);//Filename对应的是actorType//actor//currentDatetime这个文件夹
                            int rowsAffected = cmd.ExecuteNonQuery();
                            sqlConn.Close();
                            if (rowsAffected > 0)
                            {
                                dir.Parent.Delete(true);//删除datetime文件夹
                            }
                        }
                    }
                    MessageBox.Show("删除成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }

        }

        private void actor_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void actType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }



        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }


        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void img_RGB_Click(object sender, EventArgs e)
        {

        }

        private void getPosData_Resize(object sender, EventArgs e)
        {
            if (sau != null)
            {
                sau.AdaptToScreenResolution();
            }
        }

        private void ShoulderLeft_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // 指定文件的完整路径
            string filePath = dirNameBox.Text;
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

        private void getPosData_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sensor != null)
            {
                sensor.Close();
                sensor = null;
                //MessageBox.Show("摄像头已关闭");
            }
        }

        private void getPosData_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(sensor != null)
            {
                sensor.Close();
                sensor = null;
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void img_Depth_Click(object sender, EventArgs e)
        {

        }
    }
}
