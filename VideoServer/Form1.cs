using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;


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
            textBox1.Text = openFileDialog1.FileName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }



        private void button2_Click(object sender, EventArgs e)
        {
            VideoCache vc = null;
            vc = new VideoCache(openFileDialog1.FileName);
            vc.FrameLoaded += new Action<VideoCache, int>(vc_FrameLoaded);
            backgroundWorker1.RunWorkerAsync(vc);
            string abs_path=textBox3.Text;
                
            rs.addCache(abs_path, vc);
        }


        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            VideoCache vc = (VideoCache)e.Argument;
            vc.startLoad();
        }

        void vc_FrameLoaded(VideoCache arg1, int arg2)
        {
            backgroundWorker1.ReportProgress(50, arg1);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            VideoCache vc = (VideoCache)e.UserState;
            label1.Text = string.Format("已載入{0}影格", vc.frames.Count);
        }
        RTSPServer rs = null;
        private void button4_Click(object sender, EventArgs e)
        {
            int port = int.Parse(textBox2.Text);
            rs = new RTSPServer(port);
            rs.requested += new Action<Socket, string>(rs_requested);
            rs.start();
            button4.Enabled = false;
        }

        void rs_requested(Socket arg1, string arg2)
        {
            IPEndPoint remote = (IPEndPoint)arg1.RemoteEndPoint;
            string msg = string.Format("==={0}:{1} request===\n", remote.Address, remote.Port);
            appendMsg(richTextBox1, msg + arg2);
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

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(rs!=null)
                rs.close();
            Application.Exit();
        }


    }


    public class RTPPlayer
    {
        public VideoCache vc = null;
        int _seq = 0;
        int _index = 0;
        UdpSender us = null;
        bool _isPlay = false;
        byte[] buffer = new byte[100000];
        bool _isOver = false;
        public RTPPlayer(string host, int port, VideoCache vc)
        {
            us = new UdpSender(host, port);
            this.vc = vc;
        }
        double update_time = 0;
        public int send_interval = (int)1000 / 30;


        public void sendFrame()
        {
            double ms = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
            if (ms < update_time + send_interval)
                return;
            update_time = ms;
            if (_isOver)
                return;
            if (!_isPlay)
                return;
            if (_index >= vc.frames.Count)
            {
                _isOver = true;
                _isPlay = false;
                return;
            }
            byte[] pkg = vc.frames[_index++];
            Array.Copy(pkg, buffer, pkg.Length);
            RTPModel.updateSeq(buffer, _seq++);
            int timestemp = (ushort)((ms/1000) % ushort.MaxValue);
            RTPModel.updateTimeStamp(buffer, timestemp);
            us.beginSendTo(buffer, pkg.Length);
        }
        public void stop()
        {
            _isPlay = false;
        }
        public void start(int index)
        {
            _index = index;
            _isPlay = true;
            _isOver = false;
            sendFrame();
        }
        public void start()
        {
            _isPlay = true;
            _isOver = false;
            sendFrame();
        }
    }

    public class RTSPServer
    {
        Dictionary<string, RTPPlayer> rps = new Dictionary<string, RTPPlayer>();
        Dictionary<string, VideoCache> vcs = new Dictionary<string, VideoCache>();

        public class ClientHandler
        {
            RTSPServer _server;
            Socket _sock;
            Thread t = null;
            public ClientHandler(RTSPServer server, Socket sock)
            {

                this._server = server;
                _sock = sock;
                t = new Thread(new ThreadStart(run));
            }
            public void beginHandle()
            {
                t.Start();
            }

            void run()
            {
                try
                {
                    Stream s = new NetworkStream(_sock);
                    StreamReader sr = new StreamReader(s);
                    string request = sr.ReadToEnd();
                    _server.requested.Invoke(_sock, request);
                    StreamWriter sw = new StreamWriter(s);
                    handleResp(sw, request);
                    sw.Flush();
                    _sock.Shutdown(SocketShutdown.Send);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                }
            }
            void handleResp(StreamWriter sw, string req)
            {
                try
                {
                    Regex act_rex = new Regex("^(\\S+)\\s.*", RegexOptions.Singleline);
                    Regex seq_rex = new Regex("Cseq:\\s+(\\d+)", RegexOptions.Multiline);
                    string act = act_rex.Match(req).Groups[1].Value;
                    string seq = seq_rex.Match(req).Groups[1].Value;
                    switch (act.ToLower())
                    {
                        case "setup":
                            setup(sw, req, seq);
                            break;
                        case "play":
                            play(sw, req, seq);
                            break;
                        case "pause":
                            pause(sw, req, seq);
                            break;
                        case "teardown":
                            teardown(sw, req, seq);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                    sw.WriteLine("RTSP/1.0 404 ERR");
                }
            }
            void setup(StreamWriter sw, string req, string seq)
            {
                Regex path_rex = new Regex("^\\S+\\s+(\\S+).*", RegexOptions.Singleline);
                Regex port_rex = new Regex(".*client_port=\\s+(\\d+).*", RegexOptions.Multiline);
                string path = path_rex.Match(req).Groups[1].Value;
                string port = port_rex.Match(req).Groups[1].Value;
                IPEndPoint remote = (IPEndPoint)_sock.RemoteEndPoint;
                RTPPlayer p = new RTPPlayer(remote.Address.ToString(), int.Parse(port), _server.vcs[path]);
                string session=Guid.NewGuid().ToString().Substring(0, 8);
                _server.rps.Add(session, p);
                sw.WriteLine("RTSP/1.0 200 OK");
                sw.WriteLine("CSeq: {0}", seq);
                sw.WriteLine("Session: {0}", session);
            }
            void play(StreamWriter sw, string req, string seq)
            {
                string session = getSession(req);
                RTPPlayer p = _server.rps[session];
                p.start();
                send_ok(sw, seq, session);
            }
            void pause(StreamWriter sw, string req, string seq)
            {
                string session = getSession(req);
                RTPPlayer p = _server.rps[session];
                p.stop();
                send_ok(sw, seq, session);
            }
            void teardown(StreamWriter sw, string req, string seq)
            {
                string session=getSession(req);
                RTPPlayer p = _server.rps[session];
                p.stop();
                _server.rps.Remove(session);
                send_ok(sw, seq, session);
            }
            
            string getSession(string req) {
                Regex session_rex = new Regex("^Session:\\s+(\\S+).*", RegexOptions.Multiline);
                return session_rex.Match(req).Groups[1].Value;
            }
            void send_ok(StreamWriter sw, string seq, string session)
            {
                sw.WriteLine("RTSP/1.0 200 OK");
                sw.WriteLine("CSeq: {0}", seq);
                sw.WriteLine("Session: {0}", session);
            }

        }
        TcpServer ts;
        int _port;
        Thread sendPlayer =null;
        public void sendFrame() {
            while (ts.isListen()) {
                RTPPlayer[] ps = rps.Values.ToArray();
                foreach (RTPPlayer r in ps) {
                    r.sendFrame();
                }
                Thread.Sleep(20);
            }
        }
        public event Action<Socket, string> requested;
        public RTSPServer(int port)
        {
            _port = port;
            ts = new TcpServer(_port);
            ts.Accepted += new Action<TcpServer, Socket>(ts_Accepted);
        }

        void ts_Accepted(TcpServer self, Socket sock)
        {
            self.beginAccept();
            ClientHandler c = new ClientHandler(this, sock);
            c.beginHandle();
        }
        public void start()
        {
            ts.beginAccept();
            sendPlayer = new Thread(new ThreadStart(sendFrame));
            sendPlayer.Start();
        }

        public void addCache(string abs_path, VideoCache vc)
        {
            if (vcs.ContainsKey(abs_path))
                vcs[abs_path] = vc;
            else
                vcs.Add(abs_path, vc);
        }
        public void close() {
            ts.close();
        }
    }
}
