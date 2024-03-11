using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HBUtils;
using HBUtils.FormSet;

namespace 数据采集
{
    public partial class index : Form
    {
        private  AutoAdaptForm sau;
        public index()
        {
            InitializeComponent();
            AutoAdaptForm.SetTag(this);
            sau = new AutoAdaptForm(this);
            this.DoubleBuffered = true; // 启用双缓冲
        }

        private void button1_Click(object sender, EventArgs e)
        {
            getPosData spd = new getPosData();
            spd.Show();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            PlayVideo playVideo = new PlayVideo();
            playVideo.Show();

        }

        private void index_Load(object sender, EventArgs e)
        {
            HB.FormSetting.CenterFormOnScreen(this);
        }


        private void SampleShow_Click(object sender, EventArgs e)
        {
            SampleShow sampleShow = new SampleShow();
            sampleShow.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            posDataToSke posDataToSke = new posDataToSke();
            posDataToSke.Show();

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            testModels testModels = new testModels();
            testModels.Show();
        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            
        }

    

        private void index_Resize(object sender, EventArgs e)
        {
           
            if (sau != null)
            {
                sau.AdaptToScreenResolution();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            processPosData processPosData = new processPosData();
            processPosData.Show();
        }

        private void button7_Click(object sender, EventArgs e)
        {
           actionPredict actionPredict = new actionPredict();
            actionPredict.Show();
        }

        private void sample_enlarge_Click(object sender, EventArgs e)
        {
            sampleEnlarge sampleEnlarge = new sampleEnlarge();
            sampleEnlarge.Show();
        }
    }
}
