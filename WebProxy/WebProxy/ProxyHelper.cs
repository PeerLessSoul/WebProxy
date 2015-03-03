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

    public class RequestRawObject
    {
        public string RequestCommand { get; set; }
        public String RequestRawURL { get; set; }
        public String RequestHttpVersion { get; set; }
        public Uri RequestURL { get; set; }

        public string Accept { get; set; }
        public string Accept_Language { get; set; }
        public string User_Agent { get; set; }
        public string Accept_Encoding { get; set; }
        public string Host { get; set; }


        public string DNT { get; set; }
        public string Proxy_Connection { get; set; }

        public RequestRawObject(String RequestString)
        {
            String[] QueryHeaderGroup = RequestString.Replace("\r\n", "\r").Split('\r');
            RequestRawURL = QueryHeaderGroup[0];
            String[] URLSP = QueryHeaderGroup[0].Split(' ');
            RequestCommand = URLSP[0];
            if (RequestCommand == "GET")
            {
                RequestURL = new Uri(URLSP[1]);
                RequestHttpVersion = URLSP[2];
                Accept = QueryHeaderGroup[1];
                Accept_Language = QueryHeaderGroup[2];
                User_Agent = QueryHeaderGroup[3];
                Accept_Encoding = QueryHeaderGroup[4];
                Host = QueryHeaderGroup[5];
                DNT = QueryHeaderGroup[6];
                Proxy_Connection = QueryHeaderGroup[7];
            }
            else
            {
                Host = QueryHeaderGroup[1];
                Proxy_Connection = QueryHeaderGroup[2];
                User_Agent = QueryHeaderGroup[3];
            }

        }

        public override string ToString()
        {
            String ReturnString = "";
            if (String.IsNullOrEmpty(this.RequestRawURL))
            {
                ReturnString += this.RequestRawURL + "\r\n";
            }
            if (this.RequestCommand == "GET")
            {
                #region GET
                if (String.IsNullOrEmpty(this.Accept))
                {
                    ReturnString += this.Accept + "\r\n";
                }

                if (String.IsNullOrEmpty(this.Accept_Language))
                {
                    ReturnString += this.Accept_Language + "\r\n";
                }

                if (String.IsNullOrEmpty(this.User_Agent))
                {
                    ReturnString += this.User_Agent + "\r\n";
                }

                if (String.IsNullOrEmpty(this.Accept_Encoding))
                {
                    ReturnString += this.Accept_Encoding + "\r\n";
                }

                if (String.IsNullOrEmpty(this.Host))
                {
                    ReturnString += this.Host + "\r\n";
                }

                if (String.IsNullOrEmpty(this.DNT))
                {
                    ReturnString += this.DNT + "\r\n";
                }

                if (String.IsNullOrEmpty(this.Proxy_Connection))
                {
                    ReturnString += this.Proxy_Connection + "\r\n";
                }
                #endregion
            }
            else
            {
                #region CONNECT
                if (String.IsNullOrEmpty(this.Host))
                {
                    ReturnString += this.Host + "\r\n";
                }
                if (String.IsNullOrEmpty(this.Proxy_Connection))
                {
                    ReturnString += this.Proxy_Connection + "\r\n";
                }
                if (String.IsNullOrEmpty(this.User_Agent))
                {
                    ReturnString += this.User_Agent + "\r\n";
                }
                #endregion
            }
            return ReturnString;
        }

    }

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

        private void ServerBegingAccect(IAsyncResult ar)
        {
            Socket SenderSocket = ar.AsyncState as Socket;
            Socket ClientSocket = SenderSocket.EndAccept(ar);
            if (ClientSocket != null)
            {
                Byte[] ReadClientBuffer = new Byte[1024];
                ClientSocket.BeginReceive(ReadClientBuffer, 0, ReadClientBuffer.Length, SocketFlags.None, (BR) =>
                {
                    String QueryFullString = Encoding.UTF8.GetString(ReadClientBuffer);
                    RequestRawObject RRO = new RequestRawObject(QueryFullString);
                    if(RRO.RequestURL)
                }, null);
            }
            SenderSocket.BeginAccept(ServerBegingAccect, SenderSocket);
        }

        public void Stop()
        {
            ServerStateChanged(false);
        }

    }
}
