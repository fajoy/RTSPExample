using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VideoClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        
        private void Form1_Load(object sender, EventArgs e)
        {
        }

        public void updateLabel(Label l , string text) {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Label, string>(updateLabel), new object[] { l, text });
            }
            else {
                l.Text = text;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (rc == null)
                return;
            if(rc.queue.Count>0){
                Image img = rc.queryFrame();
                pictureBox1.Image = img;
            }

        }
        public void appendMsg(RichTextBox t, string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<RichTextBox, string>(appendMsg), new object[] { t, text });
            }
            else
            {
                t.AppendText(text);
                t.ScrollToCaret();
            }
        }

        RTSPClient rc = null;
        void rc_responsed(RTSPClient arg1, string arg2)
        {
            appendMsg(richTextBox1, "===server response====\n" + arg2);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string host = textBox1.Text;
            int port = int.Parse( textBox2.Text);
            string abs_path = textBox3.Text;
            rc= new RTSPClient(host,port,abs_path);
            rc.responsed+=new Action<RTSPClient,string>(rc_responsed);
            rc.recvFrameed += new Action<ClientPlayer, int, int>(rc_recvFrameed);
            rc.setup();
        }

        void rc_recvFrameed(ClientPlayer arg1, int arg2, int arg3)
        {
            updateLabel(label1, string.Format("seq:{0},time:{1},q_size:{2}\n", arg2, arg3, arg1.queue.Count));
        }


        private void button2_Click(object sender, EventArgs e)
        {
            rc.play();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            rc.pause();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            rc.teardown();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

    }


    public class ClientPlayer
    {
        public Queue<Image> queue = new Queue<Image>();
        protected UdpRevicer ur = null;
        protected bool _isPlay = false;
        public event Action<ClientPlayer, int, int> recvFrameed;
        public ClientPlayer()
        {
            ur = new UdpRevicer();
            ur.Received += new Action<UdpRevicer, System.Net.IPEndPoint, byte[]>(ur_Received);
        }

        void ur_Received(UdpRevicer self, System.Net.IPEndPoint arg2, byte[] data)
        {
            if (!_isPlay)
                return;
            ur.beginReceive();
            RTPModel pkg = new RTPModel(data);
            queue.Enqueue(FrameHelper.getFrame(pkg.payload));
            recvFrameed.Invoke(this, pkg.SequenceNumber, pkg.TimeStamp);
        }

        public void stop()
        {
            _isPlay = false;
        }
        public void start()
        {
            queue.Clear();
            _isPlay = true;
            ur.beginReceive();
        }
        public Image queryFrame()
        {

            if (queue.Count == 0)
                return null;
            return queue.Dequeue();
        }


    }
    public class RTSPClient : ClientPlayer
    {
        TcpClient tc = new TcpClient();
        string _host;
        int _port;
        int Csep;
        public string abs_path = "";
        string session = "";
        public event Action<RTSPClient, string> responsed;
        public RTSPClient(string serverHost,int serverPort,string abs_path) {
            _host = serverHost;
            _port = serverPort;
            this.abs_path = abs_path;
            tc.Connected+=new Action<TcpClient,Socket>(tc_Connected);
        }
        enum requestType {setup, play,pause,teardown}
        requestType act = requestType.setup;
        void tc_Connected(TcpClient arg1, System.Net.Sockets.Socket sock)
        {
            try
            {
                if (!sock.Connected)
                    return;
                Stream s = new NetworkStream(sock);
                StreamWriter sw = new StreamWriter(s);
                switch (act)
                {
                    case requestType.setup:
                        sw.WriteLine("SETUP {0} RTSP/1.0", abs_path);
                        sw.WriteLine("Cseq: {0}", Csep++);
                        sw.WriteLine("Transport: RTP/UDP; client_port=  {0}", this.ur.getLocalPort());
                        break;
                    case requestType.play:
                        sw.WriteLine("PLAY {0} RTSP/1.0", abs_path);
                        sw.WriteLine("Cseq: {0}", Csep++);
                        sw.WriteLine("Session: {0}", session);
                        break;
                    case requestType.pause:
                        sw.WriteLine("PAUSE {0} RTSP/1.0", abs_path);
                        sw.WriteLine("Cseq: {0}", Csep++);
                        sw.WriteLine("Session: {0}", session);
                        break;
                    case requestType.teardown:
                        sw.WriteLine("TEARDOWN {0} RTSP/1.0", abs_path);
                        sw.WriteLine("Cseq: {0}", Csep++);
                        sw.WriteLine("Session: {0}", session);
                        break;
                }
                sw.Flush();
                sock.Shutdown(SocketShutdown.Send);
                StreamReader sr = new StreamReader(s);
                string respose = sr.ReadToEnd();
                Regex session_rex = new Regex("^Session:\\s+(\\S+).*", RegexOptions.Multiline);
                session = session_rex.Match(respose).Groups[1].Value;
                responsed.Invoke(this, respose);
            }
            catch (Exception e) {
                Debug.WriteLine(e.StackTrace);
            }
        }

        public void setup() {
            act = requestType.setup;           
            Csep = 1;
            this.start();
            tc.ConnectSocket(_host, _port);
            
        }
        public void play()
        {
            act = requestType.play;
            tc.ConnectSocket(_host, _port);
        }
        public void pause()
        {
            act = requestType.pause;
            tc.ConnectSocket(_host, _port);
        }
        public void teardown()
        {
            act = requestType.teardown;
            tc.ConnectSocket(_host, _port);
        }
    }
   
}
