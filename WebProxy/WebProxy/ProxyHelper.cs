using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebProxy
{



    public class ProxyServer
    {
        Socket ServerSocket;
        Socket ClientSocket;
        IPAddress ServerIP;
        Int32 ServerPort;
        public Action<Boolean> ServerStateChanged;

        public ProxyServer(IPAddress _ServerIP, Int32 _ServerPort)
        {
            this.ServerIP = _ServerIP;
            this.ServerPort = _ServerPort;
        }

        public void Start()
        {
            try
            {
                IPEndPoint ServerIPPoint = new IPEndPoint(ServerIP.MapToIPv4(), ServerPort);
                ServerSocket = new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(ServerIPPoint);
                ServerSocket.Listen(0);
                Thread ListenThread = new Thread(new ParameterizedThreadStart(StartLinsten));
                ListenThread.Start(ServerSocket);
                ServerStateChanged(true);
            }
            catch
            {
                ServerStateChanged(false);
            }
        }

        private void StartLinsten(object obj)
        {
            Socket Socket = obj as Socket;
            Socket.BeginAccept(ServerBegingAccect, Socket);
        }
        byte[] ReviceBuffer = new byte[4096];
        private void ServerBegingAccect(IAsyncResult ar)
        {
            //服务端接受到客户端连接
            Socket SenderSocket = ar.AsyncState as Socket;
            ClientSocket = SenderSocket.EndAccept(ar);
            //取出客户端的请求
            Array.Clear(ReviceBuffer, 0, ReviceBuffer.Length);
            ClientSocket.BeginReceive(ReviceBuffer, 0, ReviceBuffer.Length, SocketFlags.None, OnClientReviced, ClientSocket);
            SenderSocket.BeginAccept(ServerBegingAccect, SenderSocket);
        }

        private void OnClientReviced(IAsyncResult ar)
        {
            Socket SenderSocket = ar.AsyncState as Socket;
            Int32 ReviceLength = SenderSocket.EndReceive(ar);
            String ReviceText = Encoding.UTF8.GetString(ReviceBuffer);

            //转法请求
            RequestRawObject RRO = new RequestRawObject(ReviceText);
            Socket AimSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (RRO.RequestURL==null)
            {
                return;
            }
            AimSocket.Connect(new IPEndPoint(Dns.GetHostAddresses(RRO.RequestURL.Host)[0], 80));
            if (AimSocket.Connected)
            {
                AimSocket.BeginSend(ReviceBuffer, 0, ReviceBuffer.Length, SocketFlags.None, OnWebResopnse, AimSocket);
            }
        }
        String RedirectResponseText;
        Int32 ReviceLength;
        private void OnWebResopnse(IAsyncResult ar)
        {
            Socket SenderSocket = ar.AsyncState as Socket;
            SenderSocket.EndSend(ar);
            //读取转发回来结果
            RedirectResponseText = "";
            ReviceLength = 0;
            Array.Clear(ReviceBuffer, 0, ReviceBuffer.Length);
            SenderSocket.BeginReceive(ReviceBuffer, 0, ReviceBuffer.Length, SocketFlags.None, OnServerRedirectResopnse, SenderSocket);
        }
        private void OnServerRedirectResopnse(IAsyncResult ar)
        {
            Socket SenderSocket = ar.AsyncState as Socket;
            ReviceLength = SenderSocket.EndReceive(ar);
            RedirectResponseText += Encoding.UTF8.GetString(ReviceBuffer);
            while (ReviceLength > 0)
            {
                SenderSocket.BeginReceive(ReviceBuffer, 0, ReviceBuffer.Length, SocketFlags.None, OnServerRedirectResopnse, SenderSocket);
                return;
            }
            ReviceBuffer = Encoding.UTF8.GetBytes(RedirectResponseText);
            ClientSocket.BeginSend(ReviceBuffer, 0, ReviceBuffer.Length, SocketFlags.None, OnClientReviced, ClientSocket);
        }



        public void Stop()
        {
            ServerStateChanged(false);
        }

    }
}
