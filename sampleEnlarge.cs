using HB.FormSettings;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static 数据采集.posDataToSke;



using HBUtils.FormSet;
using System.Diagnostics;
using System.CodeDom.Compiler;

namespace 数据采集
{
    public partial class sampleEnlarge : Form
    {
        private static string Connect3DAction = "sqlconnstring2";
        private static string ConnectSkeletonDataSet = "sqlconnstring1";

        private string sqlselect2 = "select * from table_1 where Actor=@Actor and VideoType=@ActType order by filename,cast(FrameOrder as int)";// 自己的数据库
        private string sqlselect1 = "select * from HanYueDailyAction3D where actor=@Actor and type=@ActType and filename=@FileName order by filename,cast(FrameOrder as int)";//老师的数据库
        private string sqlselect3 = "select * from Florence3DActions";

        private static Dictionary<JointType, Joint> dicJoints = new Dictionary<JointType, Joint>();
        private static Dictionary<JointType, Joint> ScaledicJoints = new Dictionary<JointType, Joint>();

        private static List<Dictionary<JointType, Joint>> dicJointsList = new List<Dictionary<JointType, Joint>>();//用于存放一个动作类型的所有骨骼数据
        private static List<Dictionary<JointType, Joint>> ScaledicJointsList = new List<Dictionary<JointType, Joint>>();//用于存放一个动作类型的所有骨骼数据

        private int currentFrame = 0;  //当前帧；
        private int totalFrames;//一个动作的总帧数

        private float times = 1;
        private Bitmap previousFrame; // 用于缓存前一帧的图像
        private bool isRuning;

        private readonly float x; //定义当前窗体的宽度
        private readonly float y; //定义当前窗体的高度

        private ScreenAdaptationUtility sau;

        string excludeJoints = "ThumbLeft,ThumbRight";//排除这两个关节点
        static string posfilePath;
        public sampleEnlarge()
        {
            InitializeComponent();

            timer1.Interval = 33;//计时器33ms执行一次
            x = Width;//最初加载窗体的宽
            y = Height;//最初加载窗体的宽
            ScreenAdaptationUtility.SetTag(this);//加载当前窗体所有组件
            sau = new ScreenAdaptationUtility(this);
        }
        private void posDataToSke_Load(object sender, EventArgs e)
        {


        }
        private void setFormLocation()  //打开窗体就居中
        {
            HB.FormSetting.CenterFormOnScreen(this);
        }
        //给定动作人和动作类型，查询唯一样本，返回Filename
        private void return_Filename()
        {
            string actType=actTypeBox.Text;
            string actor=actorBox.Text;
            string selectStr = @"SELECT distinct filename FROM HanYueDailyAction3D WHERE actor =@Actor AND type = @ActType  AND filename LIKE '202%';";//老师的数据库

            SqlConnection conn = new SqlConnection(King.StringOper.getConfigValue(Connect3DAction));
            DataTable dt = new DataTable();
            try
            {
                conn.Open();
                if (conn.State != ConnectionState.Open)
                {
                    MessageBox.Show("数据库未连接");
                }
                else
                {
                    SqlCommand sqlCommand = new SqlCommand(selectStr, conn);
                    sqlCommand.Parameters.AddWithValue("@Actor", actor);
                    sqlCommand.Parameters.AddWithValue("@ActType", actType);
                    SqlDataAdapter sda = new SqlDataAdapter(sqlCommand);
                    sda.Fill(dt);
                    //SqlDataAdapter sda = new SqlDataAdapter(selectStr, conn);
                    //sda.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        List<string> tempfileNames = new List<string>();
                        foreach (DataRow dr in dt.Rows)
                        {
                            string tempfileName = dr[0].ToString();
                            tempfileNames.Add(tempfileName);

                        }
                        HashSet<string> Filename = new HashSet<string>(tempfileNames);
                        filename.Items.Clear();
                        foreach (string k in Filename)
                        {
                            filename.Items.Add(k);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }
        //查询actor和actType和Filename
        private void returnAcotr_ActType_FileName()
        {
            string selectStr = "select distinct actor,type,filename from HanYueDailyAction3D ";//老师的数据库
            //selectStr = "select distinct actor,videotype,filename from table_1 where filename like '20241819%' ";//自己数据库，换数据库Connect3DAction也要改
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
                        List<string> tempfileNames = new List<string>();
                        foreach (DataRow dr in dt.Rows)
                        {
                            string tempActor = dr[0].ToString();
                            tempActors.Add(tempActor);
                            string tempActType = dr[1].ToString();
                            tempActTypes.Add(tempActType);
                            string tempfileName = dr[2].ToString();
                            tempfileNames.Add(tempfileName);

                        }
                        HashSet<string> Actors = new HashSet<string>(tempActors);//去除重复的动作和动作人
                        HashSet<string> ActTypes = new HashSet<string>(tempActTypes);
                        HashSet<string> Filename = new HashSet<string>(tempfileNames);
                        actorBox.Items.Clear();
                        actTypeBox.Items.Clear();
                        filename.Items.Clear();
                        foreach (string i in Actors)
                        {
                            actorBox.Items.Add(i);
                        }
                        foreach (string j in ActTypes)
                        {
                            actTypeBox.Items.Add(j);
                        }
                        foreach (string k in Filename)
                        {
                            filename.Items.Add(k);
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
        private DataSet Query(string sql, string actor, string actType,string fileName)
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
                    sqlCommand.Parameters.AddWithValue("@FileName", fileName);
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



        private void prepare()
        {
            prepareSkeJoint();//将骨骼3D坐标处理好并存入链表中

            //获取动作类型的帧数
            SqlConnection conn = new SqlConnection(King.StringOper.getConfigValue(Connect3DAction));
            DataTable dt = new DataTable();
            string sql = "select count(*) as totalFrames from HanYueDailyAction3D where Actor=@Actor and Type=@ActType and filename=@fileName ";//老师数据库
            //sql = "select count(*) as totalFrames from table_1 where Actor=@Actor and VideoType=@ActType and Filename=@fileName";//自己数据库

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
                    sqlCommand.Parameters.AddWithValue("@ActType", actTypeBox.Text);//老师数据库
                    sqlCommand.Parameters.AddWithValue("@fileName", filename.Text);//找唯一样本

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
        //处理骨骼3D坐标得到数组 dicJoints

        private void prepareSkeJoint()
        {
            dicJointsList.Clear();//清除上一次留下的骨架信息
            string actor = actorBox.Text;
            string actType = actTypeBox.Text;
            string fileName = filename.Text;
            DataSet ds = new DataSet();
            ds = Query(sqlselect1, actor, actType,fileName);
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
                        dicJointsList.Add(new Dictionary<JointType, Joint>(dicJoints));//得到一个动作类型的所有3D骨架信息
                        dicJoints.Clear(); // 现在清空dicJoints不会影响dicJointsList中的元素

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
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
        //双缓冲
        private void paintSkeleton_buffer(Dictionary<JointType, Joint> joints, PictureBox picture)
        {
            textBox1.Text = currentFrame.ToString();
            if (previousFrame == null)
            {
                // 如果前一帧缓存为空，创建一个新的缓存
                previousFrame = new Bitmap(picture.Width, picture.Height);
            }
            using (Graphics g = Graphics.FromImage(previousFrame))
            {
                g.Clear(System.Drawing.Color.AliceBlue); // 清除背景
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
            // 在一次性将前一帧缓存绘制到Panel上
            //using (Graphics g = skeleton_canvas.CreateGraphics())
            //{
            //    g.DrawImage(previousFrame, 0, 0);
            //}
            if (previousFrame != null)
            {
                picture.Image = (Image)previousFrame.Clone();
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
        public class CameraPointToColorSpace
        {
            public float X { get; set; }
            public float Y { get; set; }
            public CameraPointToColorSpace(Joint joint)
            {
                get2D(joint);
            }
            //将3D坐标转化为2D
            private void get2D(Joint joint)
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
            cp.X = (int)(cptcs.X * 250) + img_Skeleton.Width / 2;
            cp.X = cp.X < 0 ? 0 : cp.X;
            cp.X = cp.X > img_Skeleton.Width ? img_Skeleton.Width : cp.X;

            cp.Y = (int)(cptcs.Y * 250) + (int)(img_Skeleton.Height / 2.5);
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
                List<Joint> kinectJoints = new List<Joint>();
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
            suspendOrstart.Enabled = false;
        }

        private void actorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            prepareButton.Enabled = true;
            startButton.Enabled = false;
            suspendOrstart.Enabled = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            suspendOrstart.Enabled = false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Start();
            suspendOrstart.Enabled = true;
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


        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        private void img_Skeleton_Click(object sender, EventArgs e)
        {

        }

        private void presumeButton_Click(object sender, EventArgs e)
        {
            timer1.Start();
            suspendOrstart.Enabled = true;
        }

        private void prepareButton_Click(object sender, EventArgs e)
        {
            if ((string.IsNullOrEmpty(actTypeBox.Text) || string.IsNullOrEmpty(actorBox.Text))||string.IsNullOrEmpty(filename.Text))
            {
                MessageBox.Show("动作人和动作类型和文件名不能为空");
            }
            else
            {
                scaleData.PerformClick();
                timer1.Stop();
                startButton.Enabled = true;
                currentFrame = 0;
                prepare();
            }
        }

        private void sampleEnlarge_Load(object sender, EventArgs e)
        {
            setFormLocation();
            scaleFactor_X.Text = "1.2";
            scaleFactor_Y.Text = "1.2";
            scale_Steplength.Text = "0.01";

            startButton.Enabled = false;
            suspendOrstart.Enabled = false;
            //this.WindowState = FormWindowState.Maximized;
            returnAcotr_ActType_FileName();//获取数据库表中动作人和动作类型
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            times = 1;
            suspendOrstart.Enabled = true;
            isRuning = true;
            timer1.Interval = 40;//控制速度
            currentFrame = 0;
            #region 处理步骤
            //1、连接数据库
            //2、选择动作人和动作类型
            //3、查询表格
            //4、将查询到的3D坐标封装成Joint类型
            //5、将3D转化为2D
            //6、将2D展示到控件中
            #endregion
            timer1.Start();
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            currentFrame++;
            if (currentFrame < totalFrames)
            {
                try
                {
                    paintSkeleton_buffer(dicJointsList[currentFrame], img_Skeleton);//未缩放的骨架
                    paintSkeleton_buffer(ScaledicJointsList[currentFrame], img_ScaledSkeleton);//展示缩放后的骨架
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        private void sampleEnlarge_Resize(object sender, EventArgs e)
        {
            if (sau != null)
            {
                sau.AdaptToScreenResolution();
            }
        }
        public double NextDouble(Random ran, double minValue, double maxValue)
        {
            return ran.NextDouble() * (maxValue - minValue) + minValue;
        }
        private void scaleData_Click(object sender, EventArgs e)
        {

            LinkedList<string> scaleJoints = new LinkedList<string>();
            LinkedList<LinkedList<string>> scalePosData = new LinkedList<LinkedList<string>>();
            #region 处理骨骼数据
            double scale_X = double.Parse(scaleFactor_X.Text);
            double scale_Y = double.Parse(scaleFactor_Y.Text);
            Random ran = new Random();
            double randNum = NextDouble(ran, -0.02, 0.02);
            scale_Y = scale_X + randNum;
            Console.WriteLine("Y" + scale_Y);
            ScaledicJointsList.Clear();//清除上一次留下的骨架信息
            string actor = actorBox.Text;
            string actType = actTypeBox.Text;
            string fileName = filename.Text;
            DataSet ds = new DataSet();
            ds = Query(sqlselect1, actor, actType, fileName);
            if (ds != null)
            {
                try
                {
                    DataTable dt = ds.Tables[0];
                    DataRow dr = null;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dr = dt.Rows[i];
                        string SpineBase_3Dpos = ScaleCoordinates(dr["SpineBase"].ToString(), scale_X, scale_Y);
                        CustomJoint SpineBase = JointsProcess("SpineBase", SpineBase_3Dpos);

                        string SpineMid_3Dpos = ScaleCoordinates(dr["SpineMid"].ToString(), scale_X, scale_Y);
                        CustomJoint SpineMid = JointsProcess("SpineMid", SpineMid_3Dpos);

                        string Neck_3Dpos = ScaleCoordinates(dr["Neck"].ToString(), scale_X, scale_Y);
                        CustomJoint Neck = JointsProcess("Neck", Neck_3Dpos);

                        string Head_3Dpos = ScaleCoordinates(dr["Head"].ToString(), scale_X, scale_Y);
                        CustomJoint Head = JointsProcess("Head", Head_3Dpos);

                        string ShoulderLeft_3Dpos = ScaleCoordinates(dr["ShoulderLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint ShoulderLeft = JointsProcess("ShoulderLeft", ShoulderLeft_3Dpos);

                        string ElbowLeft_3Dpos = ScaleCoordinates(dr["ElbowLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint ElbowLeft = JointsProcess("ElbowLeft", ElbowLeft_3Dpos);

                        string WristLeft_3Dpos = ScaleCoordinates(dr["WristLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint WristLeft = JointsProcess("WristLeft", WristLeft_3Dpos);

                        string HandLeft_3Dpos = ScaleCoordinates(dr["HandLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint HandLeft = JointsProcess("HandLeft", HandLeft_3Dpos);

                        string ShoulderRight_3Dpos = ScaleCoordinates(dr["ShoulderRight"].ToString(), scale_X, scale_Y);
                        CustomJoint ShoulderRight = JointsProcess("ShoulderRight", ShoulderRight_3Dpos);

                        string ElbowRight_3Dpos = ScaleCoordinates(dr["ElbowRight"].ToString(), scale_X, scale_Y);
                        CustomJoint ElbowRight = JointsProcess("ElbowRight", ElbowRight_3Dpos);

                        string WristRight_3Dpos = ScaleCoordinates(dr["WristRight"].ToString(), scale_X, scale_Y);
                        CustomJoint WristRight = JointsProcess("WristRight", WristRight_3Dpos);

                        string HandRight_3Dpos = ScaleCoordinates(dr["HandRight"].ToString(), scale_X, scale_Y);
                        CustomJoint HandRight = JointsProcess("HandRight", HandRight_3Dpos);

                        string HipLeft_3Dpos = ScaleCoordinates(dr["HipLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint HipLeft = JointsProcess("HipLeft", HipLeft_3Dpos);

                        string KneeLeft_3Dpos = ScaleCoordinates(dr["KneeLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint KneeLeft = JointsProcess("KneeLeft", KneeLeft_3Dpos);

                        string AnkleLeft_3Dpos = ScaleCoordinates(dr["AnkleLeft"].ToString(), scale_X, 1);//脚踝高度不缩放
                        CustomJoint AnkleLeft = JointsProcess("AnkleLeft", AnkleLeft_3Dpos);

                        string FootLeft_3Dpos = ScaleCoordinates(dr["FootLeft"].ToString(), scale_X, 1);//脚高度不缩放
                        CustomJoint FootLeft = JointsProcess("FootLeft", FootLeft_3Dpos);

                        string HipRight_3Dpos = ScaleCoordinates(dr["HipRight"].ToString(), scale_X, scale_Y);
                        CustomJoint HipRight = JointsProcess("HipRight", HipRight_3Dpos);

                        string KneeRight_3Dpos = ScaleCoordinates(dr["KneeRight"].ToString(), scale_X, scale_Y);
                        CustomJoint KneeRight = JointsProcess("KneeRight", KneeRight_3Dpos);

                        string AnkleRight_3Dpos = ScaleCoordinates(dr["AnkleRight"].ToString(), scale_X, 1);
                        CustomJoint AnkleRight = JointsProcess("AnkleRight", AnkleRight_3Dpos);

                        string FootRight_3Dpos = ScaleCoordinates(dr["FootRight"].ToString(), scale_X, 1);
                        CustomJoint FootRight = JointsProcess("FootRight", FootRight_3Dpos);

                        string SpineShoulder_3Dpos = ScaleCoordinates(dr["SpineShoulder"].ToString(), scale_X, scale_Y);
                        CustomJoint SpineShoulder = JointsProcess("SpineShoulder", SpineShoulder_3Dpos);

                        string HandTipLeft_3Dpos = ScaleCoordinates(dr["HandTipLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint HandTipLeft = JointsProcess("HandTipLeft", HandTipLeft_3Dpos);

                        string ThumbLeft_3Dpos = ScaleCoordinates(dr["ThumbLeft"].ToString(), scale_X, scale_Y);
                        CustomJoint ThumbLeft = JointsProcess("ThumbLeft", ThumbLeft_3Dpos);

                        string HandTipRight_3Dpos = ScaleCoordinates(dr["HandTipRight"].ToString(), scale_X, scale_Y);
                        CustomJoint HandTipRight = JointsProcess("HandTipRight", HandTipRight_3Dpos);

                        string ThumbRight_3Dpos = ScaleCoordinates(dr["ThumbRight"].ToString(), scale_X, scale_Y);
                        CustomJoint ThumbRight = JointsProcess("ThumbRight", ThumbRight_3Dpos);


                        #region 将CustomJoint存入列表
                        List<CustomJoint> customJoints = new List<CustomJoint>
                        {
                            SpineBase,
                            SpineMid,
                            Neck,
                            Head,
                            ShoulderLeft,
                            ElbowLeft,
                            WristLeft,
                            HandLeft,
                            ShoulderRight,
                            ElbowRight,
                            WristRight,
                            HandRight,
                            HipLeft,
                            KneeLeft,
                            AnkleLeft,
                            FootLeft,
                            HipRight,
                            KneeRight,
                            AnkleRight,
                            FootRight,
                            SpineShoulder,
                            HandTipLeft,
                            ThumbLeft,
                            HandTipRight,
                            ThumbRight
                        };

                        #endregion
                        posJointDataProcessor posJointDataProcessor = new posJointDataProcessor();
                        List<Joint> kinectJoints = posJointDataProcessor.ConvertCustomJointsToKinectJoints(customJoints);//将自定义骨架节点数组转化为Joint数组类型，一个链表为1帧
                        foreach (Joint j in kinectJoints)
                        {
                            ScaledicJoints.Add(j.JointType, j);
                        }
                        ScaledicJointsList.Add(new Dictionary<JointType, Joint>(ScaledicJoints));//得到一个动作类型的所有3D骨架信息
                        ScaledicJoints.Clear();
                    }
                    MessageBox.Show("骨架数据缩放完毕");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(ex.StackTrace + ex.Message);
                }
            }
            #endregion
        }

        // 线性缩放三维坐标的函数
        static string ScaleCoordinates(string originalData, double scale_x, double scale_y)
        {
            if (scale_x <= 0 || scale_y <= 0)
            {
                MessageBox.Show("缩放倍数不能为零或负数");
                return originalData;
            }
            string[] coordinates = originalData.Split(',');
            if (coordinates.Length == 3)
            {
                double x = double.Parse(coordinates[0]);
                double y = double.Parse(coordinates[1]);
                double z = double.Parse(coordinates[2]);

                y = y + 0.5;//坐标系整体上移0.5,处理spinebase脊柱中心Y轴坐标接近0时缩放

                // 进行缩放
                //if (y >= 0) //骨骼点在xy平面上方，应该乘以缩放系数
                //{

                x *= scale_x;

                y *= scale_y;
                //}
                //else//骨骼点在xy平面下方，为负数，应该除以缩放系数
                //{
                //    x *= scale_x;
                //    y /= scale_y;
                //}
                y = y - 0.5;
                return $"{x},{y},{z}";
            }
            else
            {
                // 处理数据格式错误的情况,因为文本可能存在科学计数法的数据
                MessageBox.Show("存在无效、非数字的格式");
                return originalData;
            }
        }

        private void img_ScaledSkeleton_Click(object sender, EventArgs e)
        {

        }

        private void suspendButton_Click(object sender, EventArgs e)
        {
            presumeOrsuspend();
        }
        public void presumeOrsuspend()
        {
            if (isRuning)//暂停
            {
                timer1.Stop();
                isRuning = false;
            }
            else
            {
                timer1.Start();
                isRuning = true;
            }
        }

        private void saveSample_Click(object sender, EventArgs e)
        {
            if (scale_Steplength.Text == "")
            {
                MessageBox.Show("请选择缩放步长");
                return;
            }
            double stepLength = double.Parse(scale_Steplength.Text);
            #region 处理骨骼数据
            double scale_X = double.Parse(scaleFactor_X.Text);
            double scale_Y = double.Parse(scaleFactor_Y.Text);
            ScaledicJointsList.Clear();//清除上一次留下的骨架信息
            string actor = actorBox.Text;
            string actType = actTypeBox.Text;
            string fileName = filename.Text;
            DataSet ds = new DataSet();
            ds = Query(sqlselect1, actor, actType, fileName);
            if (ds != null)
            {
                try
                {
                    DataTable dt = ds.Tables[0];
                    DataRow dr = null;

                    #region 创建文件保存路径

                    string rootPath = AppDomain.CurrentDomain.BaseDirectory;
                    string posFolder = rootPath.Split(new string[] { "数据采集" }, StringSplitOptions.None)[0] + "Datas" + "\\DataEnlarge";
                    DirectoryInfo dir = new DirectoryInfo(posFolder);//实例化一个对象指向该文件夹
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    string text = @"" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day;
                    string videoDir = text + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second;
                    //创建动作人对应的文件夹
                    dir = new DirectoryInfo(posFolder + "\\" + actTypeBox.Text + "\\" + actorBox.Text + "\\" + videoDir);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    dirNameBox.Text = dir.ToString();
                    #endregion
                    int totalSample = 0;
                    //for (Double x = 0.8; x <= scale_X; x += stepLength)
                    //{
                    for (double y = 0.8; y <= scale_Y; y += stepLength)
                    {
                        double x = y;
                        totalSample++;
                        //if (x == 1 && y == 1)//不缩放
                        //{
                        //    continue;
                        //}
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            dr = dt.Rows[i];
                            string SpineBase_3Dpos = ScaleCoordinates(dr["SpineBase"].ToString(), x, y);
                            CustomJoint SpineBase = JointsProcess("SpineBase", SpineBase_3Dpos);

                            string SpineMid_3Dpos = ScaleCoordinates(dr["SpineMid"].ToString(), x, y);
                            CustomJoint SpineMid = JointsProcess("SpineMid", SpineMid_3Dpos);

                            string Neck_3Dpos = ScaleCoordinates(dr["Neck"].ToString(), x, y);
                            CustomJoint Neck = JointsProcess("Neck", Neck_3Dpos);

                            string Head_3Dpos = ScaleCoordinates(dr["Head"].ToString(), x, y);
                            CustomJoint Head = JointsProcess("Head", Head_3Dpos);

                            string ShoulderLeft_3Dpos = ScaleCoordinates(dr["ShoulderLeft"].ToString(), x, y);
                            CustomJoint ShoulderLeft = JointsProcess("ShoulderLeft", ShoulderLeft_3Dpos);

                            string ElbowLeft_3Dpos = ScaleCoordinates(dr["ElbowLeft"].ToString(), x, y);
                            CustomJoint ElbowLeft = JointsProcess("ElbowLeft", ElbowLeft_3Dpos);

                            string WristLeft_3Dpos = ScaleCoordinates(dr["WristLeft"].ToString(), x, y);
                            CustomJoint WristLeft = JointsProcess("WristLeft", WristLeft_3Dpos);

                            string HandLeft_3Dpos = ScaleCoordinates(dr["HandLeft"].ToString(), x, y);
                            CustomJoint HandLeft = JointsProcess("HandLeft", HandLeft_3Dpos);

                            string ShoulderRight_3Dpos = ScaleCoordinates(dr["ShoulderRight"].ToString(), x, y);
                            CustomJoint ShoulderRight = JointsProcess("ShoulderRight", ShoulderRight_3Dpos);

                            string ElbowRight_3Dpos = ScaleCoordinates(dr["ElbowRight"].ToString(), x, y);
                            CustomJoint ElbowRight = JointsProcess("ElbowRight", ElbowRight_3Dpos);

                            string WristRight_3Dpos = ScaleCoordinates(dr["WristRight"].ToString(), x, y);
                            CustomJoint WristRight = JointsProcess("WristRight", WristRight_3Dpos);

                            string HandRight_3Dpos = ScaleCoordinates(dr["HandRight"].ToString(), x, y);
                            CustomJoint HandRight = JointsProcess("HandRight", HandRight_3Dpos);

                            string HipLeft_3Dpos = ScaleCoordinates(dr["HipLeft"].ToString(), x, y);
                            CustomJoint HipLeft = JointsProcess("HipLeft", HipLeft_3Dpos);

                            string KneeLeft_3Dpos = ScaleCoordinates(dr["KneeLeft"].ToString(), x, y);
                            CustomJoint KneeLeft = JointsProcess("KneeLeft", KneeLeft_3Dpos);

                            string AnkleLeft_3Dpos = ScaleCoordinates(dr["AnkleLeft"].ToString(), x, 1);//脚踝高度不缩放
                            CustomJoint AnkleLeft = JointsProcess("AnkleLeft", AnkleLeft_3Dpos);

                            string FootLeft_3Dpos = ScaleCoordinates(dr["FootLeft"].ToString(), x, 1);//脚高度不缩放
                            CustomJoint FootLeft = JointsProcess("FootLeft", FootLeft_3Dpos);

                            string HipRight_3Dpos = ScaleCoordinates(dr["HipRight"].ToString(), x, y);
                            CustomJoint HipRight = JointsProcess("HipRight", HipRight_3Dpos);

                            string KneeRight_3Dpos = ScaleCoordinates(dr["KneeRight"].ToString(), x, y);
                            CustomJoint KneeRight = JointsProcess("KneeRight", KneeRight_3Dpos);

                            string AnkleRight_3Dpos = ScaleCoordinates(dr["AnkleRight"].ToString(), x, 1);
                            CustomJoint AnkleRight = JointsProcess("AnkleRight", AnkleRight_3Dpos);

                            string FootRight_3Dpos = ScaleCoordinates(dr["FootRight"].ToString(), x, 1);
                            CustomJoint FootRight = JointsProcess("FootRight", FootRight_3Dpos);

                            string SpineShoulder_3Dpos = ScaleCoordinates(dr["SpineShoulder"].ToString(), x, y);
                            CustomJoint SpineShoulder = JointsProcess("SpineShoulder", SpineShoulder_3Dpos);

                            string HandTipLeft_3Dpos = ScaleCoordinates(dr["HandTipLeft"].ToString(), x, y);
                            CustomJoint HandTipLeft = JointsProcess("HandTipLeft", HandTipLeft_3Dpos);

                            string ThumbLeft_3Dpos = ScaleCoordinates(dr["ThumbLeft"].ToString(), x, y);
                            CustomJoint ThumbLeft = JointsProcess("ThumbLeft", ThumbLeft_3Dpos);

                            string HandTipRight_3Dpos = ScaleCoordinates(dr["HandTipRight"].ToString(), x, y);
                            CustomJoint HandTipRight = JointsProcess("HandTipRight", HandTipRight_3Dpos);

                            string ThumbRight_3Dpos = ScaleCoordinates(dr["ThumbRight"].ToString(), x, y);
                            CustomJoint ThumbRight = JointsProcess("ThumbRight", ThumbRight_3Dpos);


                            #region 将CustomJoint存入列表
                            List<CustomJoint> customJoints = new List<CustomJoint>
                                {
                                    SpineBase,
                                    SpineMid,
                                    Neck,
                                    Head,
                                    ShoulderLeft,
                                    ElbowLeft,
                                    WristLeft,
                                    HandLeft,
                                    ShoulderRight,
                                    ElbowRight,
                                    WristRight,
                                    HandRight,
                                    HipLeft,
                                    KneeLeft,
                                    AnkleLeft,
                                    FootLeft,
                                    HipRight,
                                    KneeRight,
                                    AnkleRight,
                                    FootRight,
                                    SpineShoulder,
                                    HandTipLeft,
                                    ThumbLeft,
                                    HandTipRight,
                                    ThumbRight
                                };
                            #endregion
                            posJointDataProcessor posJointDataProcessor = new posJointDataProcessor();
                            List<Joint> kinectJoints = posJointDataProcessor.ConvertCustomJointsToKinectJoints(customJoints);//将自定义骨架节点数组转化为Joint数组类型，一个链表为1帧
                            foreach (Joint j in kinectJoints)
                            {
                                ScaledicJoints.Add(j.JointType, j);
                            }
                            ScaledicJointsList.Add(new Dictionary<JointType, Joint>(ScaledicJoints));//得到一个动作类型的所有3D骨架信息
                            ScaledicJoints.Clear();

                        }
                        string ts = totalSample.ToString();
                        #region 将新样本保存
                        string posfileName = "pos.txt";
                        posfileName = $"{ts}pos.txt";
                        string posfilePath = Path.Combine(dir + "", posfileName); // 将目录和文件名组合成完整的路径
                        File.WriteAllText(posfilePath, "");

                        #region 写入文本数据
                        int frameorder = 0;
                        foreach (var o in ScaledicJointsList)
                        {
                            frameorder++;
                            string recordS = frameorder + "_";

                            #region 构建关节坐标点，写入文件
                            foreach (var j in o)
                            {
                                string jointName = j.Key.ToString();
                                string jointPosition = j.Value.Position.X + "," + j.Value.Position.Y + "," + j.Value.Position.Z;

                                recordS += jointName + ":" + jointPosition + "|";
                            }
                            RecordInfo(posfilePath, recordS);

                            #endregion
                            #endregion
                            #endregion
                        }
                        ScaledicJointsList.Clear();

                    }
                    MessageBox.Show("新增样本保存完毕");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(ex.StackTrace + ex.Message);
                }
            }
            #endregion
        }
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
        private void button1_Click(object sender, EventArgs e)
        {
            // 指定文件夹的完整路径
            string directoryPath = dirNameBox.Text;
            try
            {
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

        private void scaleFactor_X_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void scaleFactor_Y_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            return_Filename();
            filename.Text = filename.Items[0].ToString();
        }
    }
}
