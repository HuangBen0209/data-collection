using Emgu.CV.Structure;
using Emgu.CV;
using Microsoft.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using HBUtils;
using HBUtils.FormSet;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace 数据采集
{
    public partial class actionPredict : Form
    {
        KinectSensor sensor;
        int width, height, frames = 0;
        string currentRBGPath = ""; //数据存放位置的文件目录
        string excludeJoints = "ThumbLeft,ThumbRight";//排除这两个关节点，后面描述骨骼点用的到
        ArrayList allImg = new ArrayList(); //动态数组存放rgb图
        ArrayList allImg_ske = new ArrayList();//存放骨骼rgb图


        ArrayList allPos = new ArrayList(); // 用于存放追踪到的身体帧，便于后期一次性插入数据库
        AutoAdaptForm sau;//控制窗体自适应
        string posfilePath;//pos文件保存路径

        public actionPredict()
        {
            InitializeComponent();
            AutoAdaptForm.SetTag(this);//加载当前窗体所有组件
            sau = new AutoAdaptForm(this);
        }

        private void actionPredict_Load(object sender, EventArgs e)
        {

            //    img_Depth.Height = 430;
            //    img_Depth.Width = 520;
            //predict.Enabled = false;

            sensor = KinectSensor.GetDefault(); // 获取当前摄像头
            if (sensor != null)
            {
                sensor.Open();
                MessageBox.Show("摄像头开启");

                MultiSourceFrameReader mfr = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Body);
                mfr.MultiSourceFrameArrived += mfr_MultiSourceFrameArrived;
            }
            else
            {
                MessageBox.Show("未检测到摄像头，请检查Kinect摄像头连接");
            }


        }
        private void preButton_Click(object sender, EventArgs e)
        {
            predict.Enabled = true;
            #region 创建文件保存路径
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            string posFolder = rootPath.Split(new string[] { "数据采集" }, StringSplitOptions.None)[0] + "\\Datas" + "\\DataPredicts";
            DirectoryInfo dir = new DirectoryInfo(posFolder);//实例化一个对象指向该文件夹
            if (!dir.Exists)
            {
                dir.Create();
            }
            //创建动作人对应的文件夹
            dir = new DirectoryInfo(posFolder + "\\" + actor.Text);
            if (!dir.Exists)
            {
                dir.Create();
            }
            string text = @"" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day;
            string timeDir = text + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second;
            dir = new DirectoryInfo(posFolder + "\\" + actor.Text + "\\" + timeDir + "\\");
            if (!dir.Exists)
            {
                dir.Create();
            }
            posfilePath = Path.Combine(dir + "", "pos.txt");
            File.WriteAllText(posfilePath, "");

            //dir = new DirectoryInfo(posFolder + actType.Text + "\\" + actor.Text + "\\" + videoDir + "\\RGB");
            dir = new DirectoryInfo(posFolder + "\\" + actor.Text + "\\" + timeDir + "\\RGB");
            if (!dir.Exists)
            {
                dir.Create();
            }
            dir = new DirectoryInfo(posFolder + "\\" + actor.Text + "\\" + timeDir + "\\RGB_Ske");
            if (!dir.Exists)
            {
                dir.Create();
            }
            dirNameBox.Text = dir.FullName.ToString();  //将文件夹完整路径：（currentRGB的文件夹(具体时间）显示到textbox
            currentRBGPath = dir.ToString();
            #endregion

        }
        //private async void predictAction()
        //{
        //    DataTable dt = new DataTable();
        //    string sqlstr = "select * from predictSkeData where 1=2";//不查询任何数据，只查询表的结构
        //    dt = King.DataBase.SqlServer.GetDataSet(sqlstr).Tables[0];//获取Table_1表的结构
        //    int frameorder = 0;
        //    //lock (allPos);
        //    foreach (object o in allPos)
        //    {
        //        frameorder++;
        //        Body bd = o as Body;
        //        string recordS = frameorder + "_";
        //        #region 保存pos文件
        //        foreach (var joint in bd.Joints)
        //        {
        //            string jointName = joint.Key.ToString();
        //            string jonitPos = joint.Value.Position.X + "," + joint.Value.Position.Y + "," + joint.Value.Position.Z;
        //            recordS += jointName + ":" + jonitPos + "|";
        //        }
        //        RecordInfo(posfilePath, recordS);//保存文本文件
        //        #endregion

        //        #region 保存人体相关信息-将骨架存入数据库+图片
        //        //#region 构建datatable数据
        //        //DataRow dr = dt.NewRow();
        //        //dr["TableKey"] = Guid.NewGuid().ToString();//生成一个全局Uid
        //        //dr["VideoType"] = actType.Text; //动作类型
        //        //dr["FileName"] = new FileInfo(posfilePath).Directory.Name.ToString();//存放的是actType//actor的路径
        //        //dr["FrameOrder"] = frameorder;
        //        //dr["Actor"] = actor.Text;
        //        //foreach (var j in bd.Joints)
        //        //{
        //        //    dr[j.Key.ToString()] = j.Value.Position.X + "," + j.Value.Position.Y + "," + j.Value.Position.Z;
        //        //}
        //        //dt.Rows.Add(dr);
        //        //#endregion
        //        //#endregion
        //        //#region 批量插入
        //        //SqlConnection sqlConn = new SqlConnection(King.StringOper.getConfigValue("sqlconnstring"));
        //        //SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConn);

        //        //bulkCopy.DestinationTableName = "predictSkeData";//写入数据库哪个表格中
        //        //bulkCopy.BatchSize = dt.Rows.Count;// 一次性插入多少行的数据，减少数据库的插入次数

        //        //try
        //        //{
        //        //    sqlConn.Open();
        //        //    if (dt != null && dt.Rows.Count != 0)
        //        //        bulkCopy.WriteToServer(dt);
        //        //    //MessageBox.Show("插入数据完成");
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    throw ex;
        //        //}
        //        //finally
        //        //{
        //        //    sqlConn.Close();
        //        //    if (bulkCopy != null)
        //        //        bulkCopy.Close();
        //        //}
        //        #endregion

        //    }
        //    #region 调用py脚本预测
        //    string rootFolder = AppDomain.CurrentDomain.BaseDirectory;
        //    string[] spiltFolder = rootFolder.Split(new string[] { "数据采集" }, StringSplitOptions.None);
        //    string pythonScriptPath = Path.Combine((spiltFolder[0] + "数据采集\\数据采集\\Resources\\Model"), "mytest.py");//python 脚本路径
        //    pythonScriptPath = @" D:\Desktop\科研实践\Coding\数据采集\数据采集\Resources\Model\mytest.py";
        //    string pythonPath = @"C:\Users\pc\AppData\Local\Programs\Python\Python311\python.exe"; // Python解释器路径

        //    string outputdata = await ProcessPool.RunPythonScriptAsync(pythonPath, pythonScriptPath, posfilePath);

        //    MessageBox.Show(outputdata);
        //    actType.Invoke((Action)(() => actType.Text = outputdata));//将预测的动作类型展示到控件中
        //    #endregion
        //    //lock (allPos)
        //    allPos.Clear();
        //}
        private async void predict_Click(object sender, EventArgs e)
        {
            DirectoryInfo diFilePath = new DirectoryInfo(currentRBGPath);//DirectoryInfo 操作目录的类
            diFilePath = diFilePath.Parent;  //acttype//actor这个文件夹
            string posFilePath = diFilePath.FullName.ToString() + "\\pos.txt";//构建完整的文件路径
            frames = 0;
            currentRBGPath = "";
            predict.Enabled = false;//防止重复点击该按钮
            int frameorder = 0;
            foreach (object o in allPos)
            {
                frameorder++;//body对象数
                Body bd = o as Body;//将o转化为mkb对象
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
            }

            dirNameBox.Text = posFilePath;

            #region 调用py脚本预测
            string rootFolder = AppDomain.CurrentDomain.BaseDirectory;
            string[] spiltFolder = rootFolder.Split(new string[] { "数据采集" }, StringSplitOptions.None);
            string pythonScriptPath = Path.Combine((spiltFolder[0] + "数据采集\\数据采集\\Resources\\Model"), "mytest2.py");//python 脚本路径
            pythonScriptPath = @" D:\Desktop\科研实践\Coding\数据采集\数据采集\Resources\Model\mytest2.py";
            string pythonPath = @"C:\Users\pc\AppData\Local\Programs\Python\Python311\python.exe"; // Python解释器路径

            string outputdata = await ProcessPool.RunPythonScriptAsync(pythonPath, pythonScriptPath, posfilePath);

            actType.Invoke((Action)(() => actType.Text = outputdata));//将预测的动作类型展示到控件中
            MessageBox.Show("预测结果:" + outputdata);

            #endregion
            //lock (allPos)
            allPos.Clear();
        }
        private void delButton_Click_1(object sender, EventArgs e)
        {
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
                    string sqlstr = "delete from predictSkeData where FileName=@FileName";
                    using (SqlConnection sqlConn = new SqlConnection(King.StringOper.getConfigValue("sqlconnstring1")))
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
        private Bitmap CreateBitmapFromDepthData(ushort[] depthData, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);

            // 将ushort数组转换为byte数组
            byte[] byteArray = new byte[depthData.Length * sizeof(ushort)];
            Buffer.BlockCopy(depthData, 0, byteArray, 0, byteArray.Length);

            IntPtr ptr = bitmapData.Scan0;
            Marshal.Copy(byteArray, 0, ptr, byteArray.Length);

            bitmap.UnlockBits(bitmapData);

            return bitmap;
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
                            Image<Bgra, byte> cImg = new Image<Bgra, byte>(cf.FrameDescription.Width, cf.FrameDescription.Height);
                            byte[] pixels = new byte[cImg.Bytes.Count<byte>()];
                            cf.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
                            cImg.Bytes = pixels;
                            Bitmap bitmap_color = cImg.ToBitmap();
                            #endregion
                            #region 获取深度图
                            int newDepthWidth = df.FrameDescription.Width;
                            int newDepthHeight = df.FrameDescription.Height;
                            ushort[] depthPixelData = new ushort[df.FrameDescription.LengthInPixels];
                            try
                            {
                                df.CopyFrameDataToArray(depthPixelData);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message.ToString());
                            }
                            Bitmap bitmap_depth = CreateBitmapFromDepthData(depthPixelData, newDepthWidth, newDepthHeight);
                            #endregion

                            #region 显示图片至img框
                            img_RGB.Image = bitmap_color;
                            if (currentRBGPath != "")
                            {
                                allImg.Add(ResizeImage(bitmap_color, 660, 450)); //将每一帧图片加入到动态数组中
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
                                        //lock (allPos) ;
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
                                    p = new System.Drawing.Pen(System.Drawing.Color.Red, 15);
                                    using (Graphics g = Graphics.FromImage(bitmap_depth))
                                    {



                                        if (notEmpty(jHead, jNeck))
                                            g.DrawLine(p, coordinateDepth_change(jHead), coordinateDepth_change(jNeck));

                                        if (notEmpty(jNeck, jSpineShoulder))
                                            g.DrawLine(p, coordinateDepth_change(jNeck), coordinateDepth_change(jSpineShoulder));

                                        if (notEmpty(jSpineShoulder, jSpineMid))
                                            g.DrawLine(p, coordinateDepth_change(jSpineShoulder), coordinateDepth_change(jSpineMid));

                                        if (notEmpty(jSpineMid, jSpineBase))
                                            g.DrawLine(p, coordinateDepth_change(jSpineMid), coordinateDepth_change(jSpineBase));

                                        #region 左半身
                                        if (notEmpty(jSpineShoulder, jShoulderLeft))
                                            g.DrawLine(p, coordinateDepth_change(jSpineShoulder), coordinateDepth_change(jShoulderLeft));

                                        if (notEmpty(jShoulderLeft, jElbowLeft))
                                            g.DrawLine(p, coordinateDepth_change(jShoulderLeft), coordinateDepth_change(jElbowLeft));

                                        if (notEmpty(jElbowLeft, jWristLeft))
                                            g.DrawLine(p, coordinateDepth_change(jElbowLeft), coordinateDepth_change(jWristLeft));

                                        if (notEmpty(jWristLeft, jHandLeft))
                                            g.DrawLine(p, coordinateDepth_change(jWristLeft), coordinateDepth_change(jHandLeft));

                                        if (notEmpty(jHandLeft, jHandTipLeft))
                                            g.DrawLine(p, coordinateDepth_change(jHandLeft), coordinateDepth_change(jHandTipLeft));

                                        if (notEmpty(jSpineBase, jHipLeft))
                                            g.DrawLine(p, coordinateDepth_change(jSpineBase), coordinateDepth_change(jHipLeft));

                                        if (notEmpty(jHipLeft, jKneeLeft))
                                            g.DrawLine(p, coordinateDepth_change(jHipLeft), coordinateDepth_change(jKneeLeft));

                                        if (notEmpty(jKneeLeft, jAnkleLeft))
                                            g.DrawLine(p, coordinateDepth_change(jKneeLeft), coordinateDepth_change(jAnkleLeft));

                                        if (notEmpty(jAnkleLeft, jFootLeft))
                                            g.DrawLine(p, coordinateDepth_change(jAnkleLeft), coordinateDepth_change(jFootLeft));
                                        #endregion

                                        #region 右半身
                                        if (notEmpty(jSpineShoulder, jShoulderRight))
                                            g.DrawLine(p, coordinateDepth_change(jSpineShoulder), coordinateDepth_change(jShoulderRight));

                                        if (notEmpty(jShoulderRight, jElbowRight))
                                            g.DrawLine(p, coordinateDepth_change(jShoulderRight), coordinateDepth_change(jElbowRight));

                                        if (notEmpty(jElbowRight, jWristRight))
                                            g.DrawLine(p, coordinateDepth_change(jElbowRight), coordinateDepth_change(jWristRight));

                                        if (notEmpty(jWristRight, jHandRight))
                                            g.DrawLine(p, coordinateDepth_change(jWristRight), coordinateDepth_change(jHandRight));

                                        if (notEmpty(jHandRight, jHandTipRight))
                                            g.DrawLine(p, coordinateDepth_change(jHandRight), coordinateDepth_change(jHandTipRight));

                                        if (notEmpty(jSpineBase, jHipRight))
                                            g.DrawLine(p, coordinateDepth_change(jSpineBase), coordinateDepth_change(jHipRight));

                                        if (notEmpty(jHipRight, jKneeRight))
                                            g.DrawLine(p, coordinateDepth_change(jHipRight), coordinateDepth_change(jKneeRight));

                                        if (notEmpty(jKneeRight, jAnkleRight))
                                            g.DrawLine(p, coordinateDepth_change(jKneeRight), coordinateDepth_change(jAnkleRight));

                                        if (notEmpty(jAnkleRight, jFootRight))
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
                                }
                            }
                            #endregion
                            if (currentRBGPath != "")
                            {
                                allImg_ske.Add(ResizeImage(bitmap_color, 660, 450)); //将每一帧图片加入到动态数组中
                                //bitmap_color.Save(currentRBGPath + "\\" + frames + ".bmp");
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

        private void dirNameBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void actionPredict_Resize(object sender, EventArgs e)
        {
            if (sau != null)
            {
                sau.AdaptToScreenResolution();
            }
        }
        private void img_Depth_Click_1(object sender, EventArgs e)
        {

        }

        private void img_RGB_Click(object sender, EventArgs e)
        {

        }

        private void delButton_Click(object sender, EventArgs e)
        {

        }

        private void startButton_Click(object sender, EventArgs e)
        {

        }

        private void actType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void actType_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void actor_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            //preButton.PerformClick();
            //predictAction();//开始预测
        }

        private void depth_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
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
                    OpenDirectory(filePath);
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

        private void actionPredict_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sensor != null)
            {
                sensor.Close();
                sensor = null;
                //MessageBox.Show("摄像头已关闭");
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            DirectoryInfo diFilePath = new DirectoryInfo(dirNameBox.Text);//DirectoryInfo 操作目录的类
            diFilePath = diFilePath.Parent;  //acttype//actor这个文件夹
            string posFilePath = diFilePath.FullName.ToString() + "\\pos.txt";//构建完整的文件路径
            #region 移动pos.txt
            string sourcePath = dirNameBox.Text;
            string destinationPath = posFilePath;
            try
            {
                File.Move(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            Console.WriteLine("文件已成功移动。");
            #endregion
            dirNameBox.Text = diFilePath.FullName;
            frames = 0;
            currentRBGPath = "";
            saveButton.Enabled = false;//防止重复点击该按钮
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

                //#region 构建关节坐标点，写入文件
                //string recordS = frameorder + "_";
                //foreach (var j in bd.Joints)
                //{
                //    string jointName = j.Key.ToString();
                //    string jointPosition = j.Value.Position.X + "," + j.Value.Position.Y + "," + j.Value.Position.Z;

                //    recordS += jointName + ":" + jointPosition + "|";
                //}
                //RecordInfo(posFilePath, recordS);
                //#endregion

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

            #region 批量插入数据库
            SqlConnection sqlConn = new SqlConnection(King.StringOper.getConfigValue("sqlconnstring"));
            SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConn);

            bulkCopy.DestinationTableName = "Table_1";//写入数据库哪个表格中
            bulkCopy.BatchSize = dt.Rows.Count;// 一次性插入多少行的数据，减少数据库的插入次数

            try
            {
                sqlConn.Open();
                if (dt != null && dt.Rows.Count != 0)
                    bulkCopy.WriteToServer(dt);
                MessageBox.Show("数据库插入数据完成");
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

            #region 保存图片数据  
            int imgOrder = 0;
            int imgOrder2 = 0;
            #region 保存RGB图像
            foreach (object o in allImg)
            {
                imgOrder++;
                string imgPath = diFilePath.FullName + "\\RGB\\" + imgOrder.ToString() + ".jpg";
                (o as Bitmap).Save(imgPath);
            }
            #endregion 
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
            allPos.Clear();

            MessageBox.Show("保存完毕。");
        }

        private void actionPredict_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sensor != null)
            {
                sensor.Close();
                sensor = null;
            }
        }

        private void actor_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void img_Depth_Click(object sender, EventArgs e)
        {

        }

    }
}
