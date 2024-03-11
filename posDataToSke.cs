using Emgu.CV.UI;
using HB.FormSettings;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;

namespace 数据采集
{
    public partial class posDataToSke : Form
    {
        private static string Connect3DAction = "sqlconnstring";
        private static string ConnectSkeletonDataSet = "sqlconnstring2";

        //private string sqlselect1 = "select * from table_1 where Actor='黄奔'and VideoType='挥手'order by filename,cast(FrameOrder as int)";
        private string sqlselect1 = "select * from table_1 where Actor=@Actor and VideoType=@ActType order by filename,cast(FrameOrder as int)";// 参数化查询
        private string sqlselect2 = "select * from HanYueDailyAction3D where actor=@Actor and type=@ActType order by filename,cast(FrameOrder as int)";
        private string sqlselect3 = "select * from Florence3DActions";

        private static Dictionary<JointType, Joint> dicJoints = new Dictionary<JointType, Joint>();
        private static List<Dictionary<JointType, Joint>> dicJointsList = new List<Dictionary<JointType, Joint>>();//用于存放一个动作类型的所有骨骼数据

        private int currentFrame = 0;  //当前帧；
        private int totalFrames;//一个动作的总帧数

        private float times = 1;
        private Bitmap previousFrame; // 用于缓存前一帧的图像


        private readonly float x; //定义当前窗体的宽度
        private readonly float y; //定义当前窗体的高度

        private ScreenAdaptationUtility sau;

        string excludeJoints = "ThumbLeft,ThumbRight";//排除这两个关节点
        public posDataToSke()
        {
            InitializeComponent();

            timer1.Interval = 40;//计时器33ms执行一次
            x = Width;//最初加载窗体的宽
            y = Height;//最初加载窗体的宽
            ScreenAdaptationUtility.SetTag(this);//加载当前窗体所有组件
            sau = new ScreenAdaptationUtility(this);
        }
        private void posDataToSke_Load(object sender, EventArgs e)
        {
            
            setFormLocation();
            timesBox.Text = "1";
            startButton.Enabled = false;
            suspendButton.Enabled = false;
            presumeButton.Enabled = false;
            //this.WindowState = FormWindowState.Maximized;
            returnAcotr_ActType();//获取数据库表中动作人和动作类型

        }
        private void setFormLocation()  //打开窗体就居中
        {
            HB.FormSetting.CenterFormOnScreen(this);
        }
        //查询actor和actType
        private void returnAcotr_ActType()
        {
            string selectStr = "select distinct actor,videotype from table_1 ";
            SqlConnection conn = new SqlConnection(King.StringOper.getConfigValue(Connect3DAction));
            DataTable dt = new DataTable();
            try
            {
                conn.Open();
                if (conn.State != ConnectionState.Open)
                {
                    MessageBox.Show("数据库连接");
                }
                else
                {
                    SqlDataAdapter sda = new SqlDataAdapter(selectStr, conn);
                    sda.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        List<string> tempActors = new List<string>();
                        List<string> tempActTypes = new List<string>();
                        foreach (DataRow dr in dt.Rows)
                        {
                            string tempActor = dr[0].ToString();
                            tempActors.Add(tempActor);
                            string tempActType = dr[1].ToString();
                            tempActTypes.Add(tempActType);

                        }
                        HashSet<string> Actors = new HashSet<string>(tempActors);//去除重复的动作和动作人
                        HashSet<string> ActTypes = new HashSet<string>(tempActTypes);
                        actorBox.Items.Clear();
                        foreach (string i in Actors)
                        {
                            actorBox.Items.Add(i);
                        }
                        foreach (string j in ActTypes)
                        {
                            actTypeBox.Items.Add(j);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }
        private DataSet Query(string sql, string actor, string actType)
        {
            SqlConnection conn = new SqlConnection(King.StringOper.getConfigValue(Connect3DAction));
            DataSet ds = new DataSet();
            try
            {
                conn.Open();
                if (conn.State != ConnectionState.Open)
                {
                    MessageBox.Show("未连接");
                }
                else
                {
                    SqlCommand sqlCommand = new SqlCommand(sql, conn);
                    sqlCommand.Parameters.AddWithValue("@Actor", actor);
                    sqlCommand.Parameters.AddWithValue("@ActType", actType);
                    SqlDataAdapter sda = new SqlDataAdapter(sqlCommand);
                    sda.Fill(ds);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            return ds;
        } //连接数据库UTKinectAction3D
          //处理3D坐标信息
        private bool isRunning = false;//控制视频循环播放
        private void button1_Click_1(object sender, EventArgs e)
        {
            times = float.Parse(timesBox.Text);
            suspendButton.Enabled = true;
            presumeButton.Enabled = false;
            timer1.Interval = (int)(40 / times);
            currentFrame = 0;
            #region 处理步骤
            //1、连接数据库
            //2、选择动作人和动作类型
            //3、查询表格
            //4、将查询到的3D坐标封装成Joint类型
            //5、将3D转化为2D
            //6、将2D展示到控件中
            timer1.Start();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if ((string.IsNullOrEmpty(actTypeBox.Text) || string.IsNullOrEmpty(actorBox.Text)))
            {
                MessageBox.Show("动作人和动作类型不能为空");
            }
            else
            {
                timer1.Stop();
                startButton.Enabled = true;
                currentFrame = 0;
                prepare();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            currentFrame++;
            if (currentFrame < totalFrames)
            {
                paintSkeleton_buffer(dicJointsList[currentFrame]);
            }
        }
        private void prepare()
        {
            prepareSkeJoint();//将骨骼3D坐标处理好并存入链表中
                              //查看动作类型的帧数
            SqlConnection conn = new SqlConnection(King.StringOper.getConfigValue(Connect3DAction));
            DataTable dt = new DataTable();
            string sql = "select count(*) as totalFrames from table_1 where Actor=@Actor and VideoType=@ActType ";
            try
            {
                conn.Open();
                if (conn.State != ConnectionState.Open)
                {
                    MessageBox.Show("未连接");
                }
                else
                {
                    SqlCommand sqlCommand = new SqlCommand(sql, conn);
                    sqlCommand.Parameters.AddWithValue("@Actor", actorBox.Text);
                    sqlCommand.Parameters.AddWithValue("@ActType", actTypeBox.Text);
                    SqlDataAdapter sda = new SqlDataAdapter(sqlCommand);
                    sda.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            DataRow dr = dt.Rows[0];
            totalFrames = int.Parse(dr[0].ToString());
        }
        //处理骨骼3D坐标得到链表 dicJoints

        private void prepareSkeJoint()
        {
            dicJointsList.Clear();//清除上一次留下的骨架信息
            string actor = actorBox.Text;
            string actType = actTypeBox.Text;
            DataSet ds = new DataSet();
            ds = Query(sqlselect1, actor, actType);
            if (ds != null)
            {
                try
                {
                    DataTable dt = ds.Tables[0];

                    DataRow dr = null;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dr = dt.Rows[i];
                        #region 构建节点
                        string SpineBise_3Dpos = dr["SpineBase"].ToString();
                        CustomJoint SpineBase = JointsProcess("SpineBase", SpineBise_3Dpos);
                        string SpineMid_3Dpos = dr["SpineMid"].ToString();
                        CustomJoint SpineMid = JointsProcess("SpineMid", SpineMid_3Dpos);

                        string Neck_3Dpos = dr["Neck"].ToString();
                        CustomJoint Neck = JointsProcess("Neck", Neck_3Dpos);

                        string Head_3Dpos = dr["Head"].ToString();
                        CustomJoint Head = JointsProcess("Head", Head_3Dpos);

                        string ShoulderLeft_3Dpos = dr["ShoulderLeft"].ToString();
                        CustomJoint ShoulderLeft = JointsProcess("ShoulderLeft", ShoulderLeft_3Dpos);

                        string ElbowLeft_3Dpos = dr["ElbowLeft"].ToString();
                        CustomJoint ElbowLeft = JointsProcess("ElbowLeft", ElbowLeft_3Dpos);

                        string WristLeft_3Dpos = dr["WristLeft"].ToString();
                        CustomJoint WristLeft = JointsProcess("WristLeft", WristLeft_3Dpos);

                        string HandLeft_3Dpos = dr["HandLeft"].ToString();
                        CustomJoint HandLeft = JointsProcess("HandLeft", HandLeft_3Dpos);

                        string ShoulderRight_3Dpos = dr["ShoulderRight"].ToString();
                        CustomJoint ShoulderRight = JointsProcess("ShoulderRight", ShoulderRight_3Dpos);

                        string ElbowRight_3Dpos = dr["ElbowRight"].ToString();
                        CustomJoint ElbowRight = JointsProcess("ElbowRight", ElbowRight_3Dpos);

                        string WristRight_3Dpos = dr["WristRight"].ToString();
                        CustomJoint WristRight = JointsProcess("WristRight", WristRight_3Dpos);

                        string HandRight_3Dpos = dr["HandRight"].ToString();
                        CustomJoint HandRight = JointsProcess("HandRight", HandRight_3Dpos);

                        string HipLeft_3Dpos = dr["HipLeft"].ToString();
                        CustomJoint HipLeft = JointsProcess("HipLeft", HipLeft_3Dpos);

                        string KneeLeft_3Dpos = dr["KneeLeft"].ToString();
                        CustomJoint KneeLeft = JointsProcess("KneeLeft", KneeLeft_3Dpos);

                        string AnkleLeft_3Dpos = dr["AnkleLeft"].ToString();
                        CustomJoint AnkleLeft = JointsProcess("AnkleLeft", AnkleLeft_3Dpos);

                        string FootLeft_3Dpos = dr["FootLeft"].ToString();
                        CustomJoint FootLeft = JointsProcess("FootLeft", FootLeft_3Dpos);

                        string HipRight_3Dpos = dr["HipRight"].ToString();
                        CustomJoint HipRight = JointsProcess("HipRight", HipRight_3Dpos);

                        string KneeRight_3Dpos = dr["KneeRight"].ToString();
                        CustomJoint KneeRight = JointsProcess("KneeRight", KneeRight_3Dpos);

                        string AnkleRight_3Dpos = dr["AnkleRight"].ToString();
                        CustomJoint AnkleRight = JointsProcess("AnkleRight", AnkleRight_3Dpos);

                        string FootRight_3Dpos = dr["FootRight"].ToString();
                        CustomJoint FootRight = JointsProcess("FootRight", FootRight_3Dpos);

                        string SpineShoulder_3Dpos = dr["SpineShoulder"].ToString();
                        CustomJoint SpineShoulder = JointsProcess("SpineShoulder", SpineShoulder_3Dpos);

                        string HandTipLeft_3Dpos = dr["HandTipLeft"].ToString();
                        CustomJoint HandTipLeft = JointsProcess("HandTipLeft", HandTipLeft_3Dpos);

                        string ThumbLeft_3Dpos = dr["ThumbLeft"].ToString();
                        CustomJoint ThumbLeft = JointsProcess("ThumbLeft", ThumbLeft_3Dpos);

                        string HandTipRight_3Dpos = dr["HandTipRight"].ToString();
                        CustomJoint HandTipRight = JointsProcess("HandTipRight", HandTipRight_3Dpos);

                        string ThumbRight_3Dpos = dr["ThumbRight"].ToString();
                        CustomJoint ThumbRight = JointsProcess("ThumbRight", ThumbRight_3Dpos);
                        #endregion
                        #region 将CustomJoint存入列表
                        List<CustomJoint> customJoints = new List<CustomJoint>();
                        customJoints.Add(SpineBase);
                        customJoints.Add(SpineMid);
                        customJoints.Add(Neck);
                        customJoints.Add(Head);
                        customJoints.Add(ShoulderLeft);
                        customJoints.Add(ElbowLeft);
                        customJoints.Add(WristLeft);
                        customJoints.Add(HandLeft);
                        customJoints.Add(ShoulderRight);
                        customJoints.Add(ElbowRight);
                        customJoints.Add(WristRight);
                        customJoints.Add(HandRight);
                        customJoints.Add(HipLeft);
                        customJoints.Add(KneeLeft);
                        customJoints.Add(AnkleLeft);
                        customJoints.Add(FootLeft);
                        customJoints.Add(HipRight);
                        customJoints.Add(KneeRight);
                        customJoints.Add(AnkleRight);
                        customJoints.Add(FootRight);
                        customJoints.Add(SpineShoulder);
                        customJoints.Add(HandTipLeft);
                        customJoints.Add(ThumbLeft);
                        customJoints.Add(HandTipRight);
                        customJoints.Add(ThumbRight);
                        #endregion
                        posJointDataProcessor posJointDataProcessor = new posJointDataProcessor();
                        List<Joint> kinectJoints = posJointDataProcessor.ConvertCustomJointsToKinectJoints(customJoints);//将自定义骨架节点数组转化为Joint数组类型，一个链表为1帧
                        foreach (Joint j in kinectJoints)
                        {
                            dicJoints.Add(j.JointType, j);
                        }
                        dicJointsList.Add(new Dictionary<JointType, Joint>(dicJoints));//得到一个动作类型的所有2D骨架信息
                        dicJoints.Clear(); // 现在清空dicJoints不会影响dicJointsList中的元素

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void Play()
        {
            int currentFrame = 1;
            if (currentFrame < totalFrames)
            {
                paintSkeleton_buffer(dicJoints);//双缓冲
                currentFrame++;
            }
        }
        private CustomJoint JointsProcess(string jointTypeName, string joint_3Dpos)
        {
            float x = 0;
            float y = 0;
            float z = 0;
            //处理坐标信息
            string[] jointXYZ = joint_3Dpos.Split(',');
            if (jointXYZ.Length == 3)
            {
                x = float.Parse(jointXYZ[0]);
                y = float.Parse(jointXYZ[1]);
                z = float.Parse(jointXYZ[2]);
            }
            posJointDataProcessor pos_Camera = new posJointDataProcessor();
            CameraSpacePoint csp = pos_Camera.Convert3DposToCameraSpacePoint(x, y, z);
            //将坐标信息赋值给对应的JointType
            if (Enum.TryParse<JointType>(jointTypeName, out JointType jointType))//将字符串解析成一个枚举类型的值
            {
                switch (jointType)
                {
                    case JointType.SpineBase:
                        CustomJoint SpineBase = new CustomJoint(JointType.SpineBase, csp);
                        return SpineBase;
                    case JointType.SpineMid:
                        CustomJoint SpineMid = new CustomJoint(JointType.SpineMid, csp);
                        return SpineMid;
                    case JointType.Neck:
                        CustomJoint Neck = new CustomJoint(JointType.Neck, csp);
                        return Neck;
                    case JointType.Head:
                        CustomJoint Head = new CustomJoint(JointType.Head, csp);
                        return Head;
                    case JointType.ShoulderLeft:
                        CustomJoint ShoulderLeft = new CustomJoint(JointType.ShoulderLeft, csp);
                        return ShoulderLeft;
                    case JointType.ElbowLeft:
                        CustomJoint ElbowLeft = new CustomJoint(JointType.ElbowLeft, csp);
                        return ElbowLeft;
                    case JointType.WristLeft:
                        CustomJoint WristLeft = new CustomJoint(JointType.WristLeft, csp);
                        return WristLeft;
                    case JointType.HandLeft:
                        CustomJoint HandLeft = new CustomJoint(JointType.HandLeft, csp);
                        return HandLeft;
                    case JointType.ShoulderRight:
                        CustomJoint ShoulderRight = new CustomJoint(JointType.ShoulderRight, csp);
                        return ShoulderRight;
                    case JointType.ElbowRight:
                        CustomJoint ElbowRight = new CustomJoint(JointType.ElbowRight, csp);
                        return ElbowRight;
                    case JointType.WristRight:
                        CustomJoint WristRight = new CustomJoint(JointType.WristRight, csp);
                        return WristRight;
                    case JointType.HandRight:
                        CustomJoint HandRight = new CustomJoint(JointType.HandRight, csp);
                        return HandRight;
                    case JointType.HipLeft:
                        CustomJoint HipLeft = new CustomJoint(JointType.HipLeft, csp);
                        return HipLeft;
                    case JointType.KneeLeft:
                        CustomJoint KneeLeft = new CustomJoint(JointType.KneeLeft, csp);
                        return KneeLeft;
                    case JointType.AnkleLeft:
                        CustomJoint AnkleLeft = new CustomJoint(JointType.AnkleLeft, csp);
                        return AnkleLeft;
                    case JointType.FootLeft:
                        CustomJoint FootLeft = new CustomJoint(JointType.FootLeft, csp);
                        return FootLeft;
                    case JointType.HipRight:
                        CustomJoint HipRight = new CustomJoint(JointType.HipRight, csp);
                        return HipRight;
                    case JointType.KneeRight:
                        CustomJoint KneeRight = new CustomJoint(JointType.KneeRight, csp);
                        return KneeRight;
                    case JointType.AnkleRight:
                        CustomJoint AnkleRight = new CustomJoint(JointType.AnkleRight, csp);
                        return AnkleRight;
                    case JointType.FootRight:
                        CustomJoint FootRight = new CustomJoint(JointType.FootRight, csp);
                        return FootRight;
                    case JointType.SpineShoulder:
                        CustomJoint SpineShoulder = new CustomJoint(JointType.SpineShoulder, csp);
                        return SpineShoulder;
                    case JointType.HandTipLeft:
                        CustomJoint HandTipLeft = new CustomJoint(JointType.HandTipLeft, csp);
                        return HandTipLeft;
                    case JointType.ThumbLeft:
                        CustomJoint ThumbLeft = new CustomJoint(JointType.ThumbLeft, csp);
                        return ThumbLeft;
                    case JointType.HandTipRight:
                        CustomJoint HandTipRight = new CustomJoint(JointType.HandTipRight, csp);
                        return HandTipRight;
                    case JointType.ThumbRight:
                        CustomJoint ThumbRight = new CustomJoint(JointType.ThumbRight, csp);
                        return ThumbRight;
                    default:
                        // 处理无法识别的情况
                        return null;
                }
            }
            return null;
        }
        //用双缓冲
        private void paintSkeleton_buffer(Dictionary<JointType, Joint> joints)
        {
            textBox1.Text = currentFrame.ToString();
            if (previousFrame == null)
            {
                // 如果前一帧缓存为空，创建一个新的缓存
                previousFrame = new Bitmap(img_Skeleton.Width, img_Skeleton.Height);
            }
            using (Graphics g = Graphics.FromImage(previousFrame))
            {
                g.Clear(System.Drawing.Color.AliceBlue); // 清除或绘制背景
                #region 将绘制好的一帧骨架存入缓冲区中                                        
                #region 画骨骼点
                System.Drawing.Brush bush = new SolidBrush(System.Drawing.Color.Red);//填充的颜色
                foreach (var j in joints)
                {
                    if (excludeJoints.IndexOf(j.Key.ToString()) == -1)//在排除的关节点里面索引所有的节点，如果没有找到，则返回-1
                    {
                        g.FillEllipse(bush, coordinateColor_change(joints[j.Key]).X - 5, coordinateColor_change(joints[j.Key]).Y - 5, 10, 10);
                    }
                  
                }
                #endregion
                #endregion
                System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.YellowGreen, 6);
                #region 关节点
                Joint jHead = joints[JointType.Head];
                Joint jNeck = joints[JointType.Neck];
                Joint jSpineShoulder = joints[JointType.SpineShoulder];
                Joint jSpineMid = joints[JointType.SpineMid];
                Joint jSpineBase = joints[JointType.SpineBase];
                #region 左半部分
                Joint jShoulderLeft = joints[JointType.ShoulderLeft];
                Joint jElbowLeft = joints[JointType.ElbowLeft];
                Joint jWristLeft = joints[JointType.WristLeft];
                Joint jHandLeft = joints[JointType.HandLeft];
                Joint jHandTipLeft = joints[JointType.HandTipLeft];
                Joint jHipLeft = joints[JointType.HipLeft];
                Joint jKneeLeft = joints[JointType.KneeLeft];
                Joint jAnkleLeft = joints[JointType.AnkleLeft];
                Joint jFootLeft = joints[JointType.FootLeft];
                #endregion
                #region 右半部分
                Joint jShoulderRight = joints[JointType.ShoulderRight];
                Joint jElbowRight = joints[JointType.ElbowRight];
                Joint jWristRight = joints[JointType.WristRight];
                Joint jHandRight = joints[JointType.HandRight];
                Joint jHandTipRight = joints[JointType.HandTipRight];
                Joint jHipRight = joints[JointType.HipRight];
                Joint jKneeRight = joints[JointType.KneeRight];
                Joint jAnkleRight = joints[JointType.AnkleRight];
                Joint jFootRight = joints[JointType.FootRight];
                #endregion 右半部分
                #endregion
                #region 连接骨骼点
                g.SmoothingMode = SmoothingMode.HighQuality; //抗锯齿高质量
                g.PixelOffsetMode = PixelOffsetMode.HighQuality; //高像素偏移质量

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
                #endregion
            }
            #endregion
            // 在一次性将前一帧缓存绘制到Panel上
            //using (Graphics g = skeleton_canvas.CreateGraphics())
            //{
            //    g.DrawImage(previousFrame, 0, 0);
            //}
            if (previousFrame != null)
            {
                img_Skeleton.Image = (Image)previousFrame.Clone();
            }
        }
        //将绘制骨骼点和连接连接骨骼点的函数集合在一起
        //private void paintSkeleton_normal(Dictionary<JointType, Joint> joints)
        //{
        //    System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.Blue, 5);
        //    #region 关节点
        //    Joint jHead = joints[JointType.Head];
        //    Joint jNeck = joints[JointType.Neck];
        //    Joint jSpineShoulder = joints[JointType.SpineShoulder];
        //    Joint jSpineMid = joints[JointType.SpineMid];
        //    Joint jSpineBase = joints[JointType.SpineBase];

        //    Joint jShoulderLeft = joints[JointType.ShoulderLeft];
        //    Joint jElbowLeft = joints[JointType.ElbowLeft];
        //    Joint jWristLeft = joints[JointType.WristLeft];
        //    Joint jHandLeft = joints[JointType.HandLeft];
        //    Joint jHandTipLeft = joints[JointType.HandTipLeft];
        //    Joint jHipLeft = joints[JointType.HipLeft];
        //    Joint jKneeLeft = joints[JointType.KneeLeft];
        //    Joint jAnkleLeft = joints[JointType.AnkleLeft];
        //    Joint jFootLeft = joints[JointType.FootLeft];

        //    Joint jShoulderRight = joints[JointType.ShoulderRight];
        //    Joint jElbowRight = joints[JointType.ElbowRight];
        //    Joint jWristRight = joints[JointType.WristRight];
        //    Joint jHandRight = joints[JointType.HandRight];
        //    Joint jHandTipRight = joints[JointType.HandTipRight];
        //    Joint jHipRight = joints[JointType.HipRight];
        //    Joint jKneeRight = joints[JointType.KneeRight];
        //    Joint jAnkleRight = joints[JointType.AnkleRight];
        //    Joint jFootRight = joints[JointType.FootRight];
        //    #endregion
        //    #region 连接骨骼点
        //    using (Graphics g = skeleton_canvas2.CreateGraphics())
        //    {
        //        g.Clear(skeleton_canvas2.BackColor); // 清除内容，用skeleton_canvas控件的背景色清除
        //        #region 画骨骼点
        //        g.SmoothingMode = SmoothingMode.HighQuality; //抗锯齿高质量
        //        g.PixelOffsetMode = PixelOffsetMode.HighQuality; //高像素偏移质量
        //        System.Drawing.Brush bush = new SolidBrush(System.Drawing.Color.Red);//填充的颜色
        //        foreach (var j in dicJoints)
        //        {
        //            g.FillEllipse(bush, coordinateColor_change(joints[j.Key]).X - 5, coordinateColor_change(joints[j.Key]).Y - 5, 10, 10);
        //        }
        //        #endregion

        //        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        //        if (notEmpty(jHead, jNeck))
        //            g.DrawLine(p, coordinateColor_change(jHead), coordinateColor_change(jNeck));

        //        if (notEmpty(jNeck, jSpineShoulder))
        //            g.DrawLine(p, coordinateColor_change(jNeck), coordinateColor_change(jSpineShoulder));

        //        if (notEmpty(jSpineShoulder, jSpineMid))
        //            g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jSpineMid));

        //        if (notEmpty(jSpineMid, jSpineBase))
        //            g.DrawLine(p, coordinateColor_change(jSpineMid), coordinateColor_change(jSpineBase));

        //        #region 左半身
        //        if (notEmpty(jSpineShoulder, jShoulderLeft))
        //            g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jShoulderLeft));

        //        if (notEmpty(jShoulderLeft, jElbowLeft))
        //            g.DrawLine(p, coordinateColor_change(jShoulderLeft), coordinateColor_change(jElbowLeft));

        //        if (notEmpty(jElbowLeft, jWristLeft))
        //            g.DrawLine(p, coordinateColor_change(jElbowLeft), coordinateColor_change(jWristLeft));

        //        if (notEmpty(jWristLeft, jHandLeft))
        //            g.DrawLine(p, coordinateColor_change(jWristLeft), coordinateColor_change(jHandLeft));

        //        if (notEmpty(jHandLeft, jHandTipLeft))
        //            g.DrawLine(p, coordinateColor_change(jHandLeft), coordinateColor_change(jHandTipLeft));

        //        if (notEmpty(jSpineBase, jHipLeft))
        //            g.DrawLine(p, coordinateColor_change(jSpineBase), coordinateColor_change(jHipLeft));

        //        if (notEmpty(jHipLeft, jKneeLeft))
        //            g.DrawLine(p, coordinateColor_change(jHipLeft), coordinateColor_change(jKneeLeft));

        //        if (notEmpty(jKneeLeft, jAnkleLeft))
        //            g.DrawLine(p, coordinateColor_change(jKneeLeft), coordinateColor_change(jAnkleLeft));

        //        if (notEmpty(jAnkleLeft, jFootLeft))
        //            g.DrawLine(p, coordinateColor_change(jAnkleLeft), coordinateColor_change(jFootLeft));
        //        #endregion

        //        #region 右半身
        //        if (notEmpty(jSpineShoulder, jShoulderRight))
        //            g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jShoulderRight));

        //        if (notEmpty(jShoulderRight, jElbowRight))
        //            g.DrawLine(p, coordinateColor_change(jShoulderRight), coordinateColor_change(jElbowRight));

        //        if (notEmpty(jElbowRight, jWristRight))
        //            g.DrawLine(p, coordinateColor_change(jElbowRight), coordinateColor_change(jWristRight));

        //        if (notEmpty(jWristRight, jHandRight))
        //            g.DrawLine(p, coordinateColor_change(jWristRight), coordinateColor_change(jHandRight));

        //        if (notEmpty(jHandRight, jHandTipRight))
        //            g.DrawLine(p, coordinateColor_change(jHandRight), coordinateColor_change(jHandTipRight));

        //        if (notEmpty(jSpineBase, jHipRight))
        //            g.DrawLine(p, coordinateColor_change(jSpineBase), coordinateColor_change(jHipRight));

        //        if (notEmpty(jHipRight, jKneeRight))
        //            g.DrawLine(p, coordinateColor_change(jHipRight), coordinateColor_change(jKneeRight));

        //        if (notEmpty(jKneeRight, jAnkleRight))
        //            g.DrawLine(p, coordinateColor_change(jKneeRight), coordinateColor_change(jAnkleRight));

        //        if (notEmpty(jAnkleRight, jFootRight))
        //            g.DrawLine(p, coordinateColor_change(jAnkleRight), coordinateColor_change(jFootRight));
        //        #endregion

        //        #endregion
        //    }
        //}
        //判断点的坐标否为零
        private bool notEmpty(Joint joint1, Joint joint2)
        {
            if ((coordinateColor_change(joint1).X != 0 && coordinateColor_change(joint1).Y != 0) &&
                (coordinateColor_change(joint2).X != 0 && coordinateColor_change(joint2).Y != 0))
                return true;
            else
                return false;
        }

        //在bitmap中试试画图效果----其实相当于双缓冲  这个要用定时器才能生效
        //private void paintSkeleton_bitmap(Dictionary<JointType, Joint> joints)
        //{
        //    Bitmap bm = new Bitmap(Skeleton_picture.Width, Skeleton_picture.Height);
        //    System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.Blue, 3);
        //    Skeleton_picture.Image = bm;
        //    #region 关节点
        //    Joint jHead = joints[JointType.Head];
        //    Joint jNeck = joints[JointType.Neck];
        //    Joint jSpineShoulder = joints[JointType.SpineShoulder];
        //    Joint jSpineMid = joints[JointType.SpineMid];
        //    Joint jSpineBase = joints[JointType.SpineBase];

        //    Joint jShoulderLeft = joints[JointType.ShoulderLeft];
        //    Joint jElbowLeft = joints[JointType.ElbowLeft];
        //    Joint jWristLeft = joints[JointType.WristLeft];
        //    Joint jHandLeft = joints[JointType.HandLeft];
        //    Joint jHandTipLeft = joints[JointType.HandTipLeft];
        //    Joint jHipLeft = joints[JointType.HipLeft];
        //    Joint jKneeLeft = joints[JointType.KneeLeft];
        //    Joint jAnkleLeft = joints[JointType.AnkleLeft];
        //    Joint jFootLeft = joints[JointType.FootLeft];

        //    Joint jShoulderRight = joints[JointType.ShoulderRight];
        //    Joint jElbowRight = joints[JointType.ElbowRight];
        //    Joint jWristRight = joints[JointType.WristRight];
        //    Joint jHandRight = joints[JointType.HandRight];
        //    Joint jHandTipRight = joints[JointType.HandTipRight];
        //    Joint jHipRight = joints[JointType.HipRight];
        //    Joint jKneeRight = joints[JointType.KneeRight];
        //    Joint jAnkleRight = joints[JointType.AnkleRight];
        //    Joint jFootRight = joints[JointType.FootRight];
        //    #endregion
        //    Graphics g = Graphics.FromImage(bm);
        //    g.FillRectangle(System.Drawing.Brushes.White, new Rectangle(0, 0, Skeleton_picture.Width, Skeleton_picture.Height));
        //    //g.Clear(skeleton_canvas.BackColor); // 清除内容，用skeleton_canvas控件的背景色清除
        //    #region 画骨骼点
        //    g.SmoothingMode = SmoothingMode.HighQuality; //抗锯齿高质量
        //    g.PixelOffsetMode = PixelOffsetMode.HighQuality; //高像素偏移质量
        //    System.Drawing.Brush bush = new SolidBrush(System.Drawing.Color.Red);//填充的颜色
        //    foreach (var j in dicJoints)
        //    {
        //        g.FillEllipse(bush, coordinateColor_change(joints[j.Key]).X - 5, coordinateColor_change(joints[j.Key]).Y - 5, 10, 10);
        //    }
        //    #endregion
        //    #region 画骨骼连接线
        //    g.DrawLine(p, coordinateColor_change(jHead), coordinateColor_change(jNeck));
        //    g.DrawLine(p, coordinateColor_change(jNeck), coordinateColor_change(jSpineShoulder));
        //    g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jSpineMid));
        //    g.DrawLine(p, coordinateColor_change(jSpineMid), coordinateColor_change(jSpineBase));

        //    #region 左半身
        //    g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jShoulderLeft));
        //    g.DrawLine(p, coordinateColor_change(jShoulderLeft), coordinateColor_change(jElbowLeft));
        //    g.DrawLine(p, coordinateColor_change(jElbowLeft), coordinateColor_change(jWristLeft));
        //    g.DrawLine(p, coordinateColor_change(jWristLeft), coordinateColor_change(jHandLeft));
        //    g.DrawLine(p, coordinateColor_change(jHandLeft), coordinateColor_change(jHandTipLeft));
        //    g.DrawLine(p, coordinateColor_change(jSpineBase), coordinateColor_change(jHipLeft));
        //    g.DrawLine(p, coordinateColor_change(jHipLeft), coordinateColor_change(jKneeLeft));
        //    g.DrawLine(p, coordinateColor_change(jKneeLeft), coordinateColor_change(jAnkleLeft));
        //    g.DrawLine(p, coordinateColor_change(jAnkleLeft), coordinateColor_change(jFootLeft));
        //    #endregion

        //    #region 右半身
        //    g.DrawLine(p, coordinateColor_change(jSpineShoulder), coordinateColor_change(jShoulderRight));
        //    g.DrawLine(p, coordinateColor_change(jShoulderRight), coordinateColor_change(jElbowRight));
        //    g.DrawLine(p, coordinateColor_change(jElbowRight), coordinateColor_change(jWristRight));
        //    g.DrawLine(p, coordinateColor_change(jWristRight), coordinateColor_change(jHandRight));
        //    g.DrawLine(p, coordinateColor_change(jHandRight), coordinateColor_change(jHandTipRight));
        //    g.DrawLine(p, coordinateColor_change(jSpineBase), coordinateColor_change(jHipRight));
        //    g.DrawLine(p, coordinateColor_change(jHipRight), coordinateColor_change(jKneeRight));
        //    g.DrawLine(p, coordinateColor_change(jKneeRight), coordinateColor_change(jAnkleRight));
        //    g.DrawLine(p, coordinateColor_change(jAnkleRight), coordinateColor_change(jFootRight));
        //    #endregion
        //    #endregion
        //    g.Dispose();
        //    Skeleton_picture.Image = bm;
        //}
        //相机空间3D坐标转化为彩色图像的2D坐标
        public class CameraPointToColorSpace
        {
            public float X { get; set; }
            public float Y { get; set; }
            public CameraPointToColorSpace(Joint joint)
            {
                CameraPointToColorSpace_1(joint);
            }
            //将3D坐标转化为2D
            private void CameraPointToColorSpace_1(Joint joint)
            {

                X = joint.Position.X / joint.Position.Z;
                Y = joint.Position.Y / -joint.Position.Z;
            }
        }
        //将2D坐标转化为可以直接在屏幕中显示的坐标
        public System.Drawing.Point coordinateColor_change(Joint joint)
        {
            System.Drawing.Point cp = new System.Drawing.Point();
            CameraPointToColorSpace cptcs = new CameraPointToColorSpace(joint);
            cp.X = (int)(cptcs.X * 400) + img_Skeleton.Width/2;
            cp.X = cp.X < 0 ? 0 : cp.X;
            cp.X = cp.X > img_Skeleton.Width ? img_Skeleton.Width : cp.X;

            cp.Y = (int)(cptcs.Y * 400) + (int)(img_Skeleton.Height /2.5);
            cp.Y = cp.Y < 0 ? 0 : cp.Y;
            cp.Y = cp.Y > img_Skeleton.Height ? img_Skeleton.Height : cp.Y;

            return cp;
        }
        public class posJointDataProcessor
        {
            public CameraSpacePoint Convert3DposToCameraSpacePoint(double x, double y, double z)
            {
                CameraSpacePoint csp = new CameraSpacePoint();
                csp.X = (float)x;
                csp.Y = (float)y;
                csp.Z = (float)z;
                return csp;
            }
            //将自定义jointList转化为kinect内置的JointList
            public List<Joint> ConvertCustomJointsToKinectJoints(List<CustomJoint> customJoints)
            {
                //dicJointsList.Add(dicJoints);
                List<Joint> kinectJoints = new List<Joint>();
                //dicJoints.Clear();//清除字典dicJoints中上一次存放的数据
                foreach (CustomJoint customJoint in customJoints)
                {
                    kinectJoints.Add(ConvertCustomJointToKinectJoint(customJoint));//将自定义的Joint存入kinect支持的Joint中，并且加入到KinectJoints列表中
                }
                return kinectJoints;
            }
            //将自定的customJoint转化为kinect内置的joint类型
            private Joint ConvertCustomJointToKinectJoint(CustomJoint customJoint)
            {
                JointType jointType = customJoint.JointType;
                CameraSpacePoint cameraPoint = customJoint.Position;

                Joint kinectJoint = new Joint
                {
                    JointType = jointType,
                    Position = new CameraSpacePoint
                    {
                        X = cameraPoint.X,
                        Y = cameraPoint.Y,
                        Z = cameraPoint.Z
                    }
                };
                return kinectJoint;
            }
        }
        public class CustomJoint
        {
            public CustomJoint(JointType jointType, CameraSpacePoint Position3D)
            {
                JointType = jointType;
                Position = Position3D;
            }
            public JointType JointType { get; set; }
            public CameraSpacePoint Position { get; set; }
        }
        private void skeleton_canvas_Paint(object sender, PaintEventArgs e)
        {

        }

        private void TypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            prepareButton.Enabled = true;
            startButton.Enabled = false;
            presumeButton.Enabled = false;
            suspendButton.Enabled = false;
        }

        private void actorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            prepareButton.Enabled = true;
            startButton.Enabled = false;
            presumeButton.Enabled = false;
            suspendButton.Enabled = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            presumeButton.Enabled = true;
            suspendButton.Enabled = false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Start();
            presumeButton.Enabled = false;
            suspendButton.Enabled = true;
        }

        private void bindingSource1_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void timesBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void posDataToSke_Resize(object sender, EventArgs e)
        {
            if (sau != null)
            {
                sau.AdaptToScreenResolution();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}