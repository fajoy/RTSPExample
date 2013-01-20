using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;


public class TcpServer
{
    TcpListener sock;
    IPEndPoint e;
    public event Action<TcpServer, Socket> Accepted;
    public TcpServer(int listenPort)
    {
        e = new IPEndPoint(IPAddress.Any, listenPort);
        sock = new TcpListener(e);

    }
    public void beginAccept()
    {
        if (!sock.Server.IsBound)
            sock.Start();
        sock.BeginAcceptSocket(ack, this);
    }
    static AsyncCallback ack = new AsyncCallback(AcceptCallback);
    static void AcceptCallback(IAsyncResult ar)
    {
        TcpServer self = (TcpServer)(ar.AsyncState);
        Socket c = null;
        try
        {
            c = self.sock.EndAcceptSocket(ar);

        }
        catch
        {

        }
        self.Accepted.Invoke(self, c);
    }

    public bool isListen()
    {
        return sock.Server.IsBound;
    }
    public void close()
    {
        try
        {
            sock.Stop();
        }
        catch { }
    }
}

public class TcpClient
{
    Socket sock;
    public event Action<TcpClient, Socket> Connected;
    static AsyncCallback cck = new AsyncCallback(ConnectCallback);
    static void ConnectCallback(IAsyncResult ar)
    {
        TcpClient self = (TcpClient)ar.AsyncState;
        if (self.sock.Connected)
            self.sock.EndConnect(ar);
        self.Connected.Invoke(self, self.sock);
    }
    public void ConnectSocket(string host, int port)
    {
        IPAddress[] ips = Dns.GetHostAddresses(host);
        IPEndPoint e = new IPEndPoint(ips[0], port);
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.BeginConnect(e, cck, this);
    }

}


public class UdpRevicer
{

    UdpClient client;
    IPEndPoint e;
    public UdpRevicer(int port)
    {
        e = new IPEndPoint(IPAddress.Any, port);
        client = new UdpClient(e);
    }
    public UdpRevicer()
    {
        client = new UdpClient(0, AddressFamily.InterNetwork);
    }
    public int getLocalPort()
    {
        return ((IPEndPoint)client.Client.LocalEndPoint).Port;
    }
    public event Action<UdpRevicer, IPEndPoint, byte[]> Received;
    static AsyncCallback rck = new AsyncCallback(ReceiveCallback);
    static void ReceiveCallback(IAsyncResult ar)
    {
        UdpRevicer self = (UdpRevicer)ar.AsyncState;
        IPEndPoint remote = null;
        byte[] receiveBytes = self.client.EndReceive(ar, ref remote);
        self.Received.Invoke(self, remote, receiveBytes);
    }


    public void beginReceive()
    {
        client.BeginReceive(rck, this);
    }

}


public class UdpSender
{
    Socket sock;
    IPEndPoint e;
    public UdpSender(string host, int port)
    {
        IPAddress[] ips = Dns.GetHostAddresses(host);
        e = new IPEndPoint(ips[0], port);
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    }


    static AsyncCallback sck = new AsyncCallback(SendToCallback);
    public static void SendToCallback(IAsyncResult ar)
    {
        UdpSender self = (UdpSender)(ar.AsyncState);
        self.sock.EndSendTo(ar);
    }


    public void beginSendTo(byte[] datagram, int bytes)
    {
        sock.BeginSendTo(datagram, 0, bytes, 0, e, sck, this);
    }

}


