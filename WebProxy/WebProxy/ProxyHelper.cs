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
        Socket AimSocket;
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
        byte[] recvBytes = new byte[4096];
        private void ServerBegingAccect(IAsyncResult ar)
        {
            Socket SenderSocket = ar.AsyncState as Socket;
           
            SenderSocket.BeginAccept(ServerBegingAccect, SenderSocket);
        }

        public void Stop()
        {
            ServerStateChanged(false);
        }

    }
}
