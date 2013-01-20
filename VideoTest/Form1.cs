using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;

namespace VideoServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = Environment.CurrentDirectory;
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;

        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            q.Clear();
            textBox1.Text = openFileDialog1.FileName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog(); 
        }

        Queue<Image<Bgr, Byte>> q = new Queue<Image<Bgr, Byte>>();
        private void button2_Click(object sender, EventArgs e)
        {
            Capture c = new Capture(openFileDialog1.FileName);
            Image<Bgr, Byte> frame = null;
            do
            {
                frame = c.QueryFrame();
                if (frame == null)
                    break;
                q.Enqueue(frame.Clone());
            } while (true);
          

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (q.Count > 0)
                pictureBox1.Image = q.Dequeue().ToBitmap();   
        }

     
    }
}
