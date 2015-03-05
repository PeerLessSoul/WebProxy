using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebProxy
{
    public class ProxyWeb3
    {
        Socket ServerSocket;
        
      
        

        public ProxyWeb3(IPAddress _ServerIP, Int32 _ServerPort)
        {
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ServerIPEndPoint = new IPEndPoint(_ServerIP, _ServerPort);
            ServerSocket.Bind(ServerIPEndPoint);
            ServerSocket.Listen(50);
            ServerSocket.BeginAccept(OnServerAccept, null);
        }
        
        private void OnServerAccept(IAsyncResult ar)
        {
            Socket ClientSocket = ServerSocket.EndAccept(ar);
            ServerSocket.BeginAccept(OnServerAccept, null);
            ProxyRevicedInfo PRI = new ProxyRevicedInfo("", 0, ClientSocket);
            PRI.RefenceClientSokcet = ClientSocket;
            ClientSocket.BeginReceive(PRI.Buffer, 0, PRI.Buffer.Length, SocketFlags.None, OnClientRequestToServer, PRI);

        }
        //http://www.cnblogs.com/xiaozhi_5638/p/3917943.html
        Int32 Count = 0;
        private void OnClientRequestToServer(IAsyncResult ar)
        {
            ProxyRevicedInfo PRI = ar.AsyncState as ProxyRevicedInfo;
            Int32 ReviceLength = PRI.RefenceSocket.EndReceive(ar);
            PRI.ResponseText += Encoding.ASCII.GetString(PRI.Buffer);
            PRI.RevicedLength = ReviceLength;
            while (PRI.RevicedLength >= PRI.Buffer.Length)
            {
                PRI.RefenceSocket.BeginReceive(PRI.Buffer, 0, PRI.Buffer.Length, SocketFlags.None, OnClientRequestToServer, PRI);
                return;
            }
            if (String.IsNullOrEmpty(PRI.ResponseText)) return;
            Byte[] RequeryBytes = Encoding.ASCII.GetBytes(PRI.ResponseText);
            //全部取出ClientRequest
            RequestRawObject RRO = new RequestRawObject(PRI.ResponseText);
            RRO.RefenceClientSocket = PRI.RefenceClientSokcet;
            RRO.RefenceBuffer = RequeryBytes;
            Int32 RemotePort = 80;
            if (RRO.RequestCommand == "CONNECT")
            {
                RemotePort = 443;
            }
            if (RRO.RequestCommand == "POST")
            {

            }

            Socket ServerSocketForCatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            RRO.RefenceSocket = ServerSocketForCatch;

            if (!String.IsNullOrEmpty(RRO.Proxy_Connection) && RRO.Proxy_Connection.ToUpper().Equals("KEEP-ALIVE"))
            {
                ServerSocketForCatch.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
            }
            ServerSocketForCatch.BeginConnect(new IPEndPoint(Dns.GetHostAddresses(RRO.RequestURL.Host)[0], RemotePort), OnServerCatchWebConnect, RRO);
        }

        private void OnServerCatchWebConnect(IAsyncResult ar)
        {
            RequestRawObject RRO = ar.AsyncState as RequestRawObject;
            RRO.RefenceSocket.EndConnect(ar);
            if (RRO.RequestCommand == "CONNECT")
            {
                String CONNECTSTATE = RRO.RequestHttpVersion + " 200 Connection established\r\n\r\n";
                RRO.RefenceClientSocket.BeginSend(Encoding.ASCII.GetBytes(CONNECTSTATE), 0, CONNECTSTATE.Length, SocketFlags.None, (X) =>
                {
                    RRO.RefenceClientSocket.EndSend(X);
                    
                    //StartRelay();
                }, null);
            }
            else
            {
                RRO.RefenceSocket.BeginSend(RRO.RefenceBuffer, 0, RRO.RefenceBuffer.Length, SocketFlags.None, OnServerCatchWebSend, RRO);
            }
        }

        private void OnServerCatchWebSend(IAsyncResult ar)
        {
            RequestRawObject RRO = ar.AsyncState as RequestRawObject;
            int SendLength = RRO.RefenceSocket.EndSend(ar);
            if (SendLength != RRO.RefenceBuffer.Length)
            {
                throw new Exception("发送有问题！");
            }
            else
            {
                ProxyRevicedInfo PRI = new ProxyRevicedInfo("", 0, RRO.RefenceSocket);
                PRI.RefenceClientSokcet = RRO.RefenceClientSocket;
                RRO.RefenceSocket.BeginReceive(PRI.Buffer, 0, PRI.Buffer.Length, SocketFlags.None, OnServerCatchWebResponse, PRI);
            }
            
        }

        private void OnServerCatchWebResponse(IAsyncResult ar)
        {
            ProxyRevicedInfo PRI = ar.AsyncState as ProxyRevicedInfo;
            Int32 ReviceLength = PRI.RefenceSocket.EndReceive(ar);
            PRI.ResponseText += Encoding.ASCII.GetString(PRI.Buffer);
            PRI.RevicedLength = ReviceLength;
            while (PRI.RevicedLength >= PRI.Buffer.Length)
            {
                PRI.RefenceSocket.BeginReceive(PRI.Buffer, 0, PRI.Buffer.Length, SocketFlags.None, OnServerCatchWebResponse, PRI);
                return;
            }
            PRI.RefenceSocket.Shutdown(SocketShutdown.Both);
            PRI.RefenceSocket.Close();
            
            Byte[] RequeryBytes = Encoding.ASCII.GetBytes(PRI.ResponseText);

            //PRI.RefenceClientSokcet.BeginSend(RequeryBytes,0, RequeryBytes.Length,SocketFlags.None, (X) => {
                
            //    ProxyRevicedInfo Client = X.AsyncState as ProxyRevicedInfo;
            //    Client.RefenceClientSokcet.EndReceive(X);
            //}, PRI);
            PRI.RefenceSocket.Dispose();
            PRI.RefenceSocket = null;
            PRI.RefenceSocket = PRI.RefenceClientSokcet;
            PRI.RefenceClientSokcet.BeginSend(RequeryBytes, 0, RequeryBytes.Length, SocketFlags.None, OnClientRequestToServer,PRI);
            Console.WriteLine(PRI.ResponseText);
        }



        
    }
}
