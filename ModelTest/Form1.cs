using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ModelTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        RTPModel p = new RTPModel(10, 20, 999, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        private void button1_Click(object sender, EventArgs e)
        {
            
            byte[] bs = p.toBytes();
            richTextBox1.Clear();
            for (int i = 0; i < bs.Length;i++ )
            {
                richTextBox1.AppendText(i.ToString()+":"+bs[i]+"\n");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            byte[] bs = p.toBytes();
            RTPModel p2 = new RTPModel(bs);
            bs = p2.toBytes();
            richTextBox1.Clear();
            for (int i = 0; i < bs.Length; i++)
            {
                richTextBox1.AppendText(i.ToString() + ":" + bs[i] + "\n");
            }
        }
    }
}
