﻿using System;
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
        /// <summary>
        /// 服务器Sicket
        /// </summary>
        Socket ServerSocket;

        /// <summary>
        /// 客户端Socket
        /// </summary>
        Socket ClientSocket;

        /// <summary>
        /// 服务端外网代理Socket
        /// </summary>
        Socket ServerSideSocket;


        byte[] RequestBuffer = new byte[0x1000];
        byte[] ResponseBuffer = new byte[0x400];

        public void CleartBuffer(byte[] SourceBuffer)
        {
            Array.Clear(SourceBuffer, 0, SourceBuffer.Length);
        }

        public void TrySocket(Action DoAction)
        {
            try
            {
                DoAction();
            }
            catch
            {
                //if (ServerSideSocket != null) ServerSideSocket.Shutdown(SocketShutdown.Both);
                if (ClientSocket != null) ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket = null;
                ServerSideSocket = null;
            }
        }

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
                ServerSocket.Listen(50);
                //Thread ListenThread = new Thread(new ParameterizedThreadStart(StartLinsten));
                //ListenThread.Start(ServerSocket);
                ServerSocket.BeginAccept(OnClientStart, null);
                ServerStateChanged(true);
            }
            catch
            {
                ServerStateChanged(false);
            }
        }
        /// <summary>
        /// 服务端开始监听接收客户端
        /// </summary>
        /// <param name="obj"></param>
        private void StartLinsten(object obj)
        {

        }


        private void OnClientStart(IAsyncResult ar)
        {
            TrySocket(() =>
            {
                //服务端接受到客户端连接
                ClientSocket = ServerSocket.EndAccept(ar);

                //取出客户端的请求
                CleartBuffer(ResponseBuffer);
                ClientSocket.BeginReceive(ResponseBuffer, 0, ResponseBuffer.Length, SocketFlags.None, OnClientResponse, null);

                //继续开始监听
                ServerSocket.BeginAccept(OnClientStart, null);
            });
        }

        private void OnClientResponse(IAsyncResult ar)
        {
            TrySocket(() =>
            {
                Int32 ReviceLength = ClientSocket.EndReceive(ar);
                String ReviceText = Encoding.ASCII.GetString(ResponseBuffer);
                Int32 ss = ReviceText.Length;

                //现内部远程外部WEB
                RequestRawObject RRO = new RequestRawObject(ReviceText);
                Int32 RemotePort = 80;
                if (RRO.RequestCommand == "CONNECT")
                {
                    RemotePort = 443;
                }
                if (RRO.RequestCommand == "POST")
                {

                }
                if (RRO.RequestURL == null)
                {
                    return;
                }

                ServerSideSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (!String.IsNullOrEmpty(RRO.Proxy_Connection) && RRO.Proxy_Connection.ToUpper().Equals("KEEP-ALIVE"))
                {
                    ServerSideSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                }

                ServerSideSocket.BeginConnect(new IPEndPoint(Dns.GetHostAddresses(RRO.RequestURL.Host)[0], RemotePort), OnServerAimSocketConnected, RRO);

                Console.WriteLine("请求连接:" + RRO.RequestRawURL);

                ClientSocket.BeginReceive(ResponseBuffer, 0, ResponseBuffer.Length, SocketFlags.None, OnClientResponse, null);
            });

        }
        //服务端再连接远程WEB
        private void OnServerAimSocketConnected(IAsyncResult ar)
        {
            TrySocket(() =>
            {
                

                RequestRawObject RRO = ar.AsyncState as RequestRawObject;
                ServerSideSocket.EndConnect(ar);
                if (RRO.RequestCommand == "CONNECT")
                {
                    String CONNECTSTATE = RRO.RequestHttpVersion + " 200 Connection established\r\n\r\n";
                    ClientSocket.BeginSend(Encoding.ASCII.GetBytes(CONNECTSTATE), 0, CONNECTSTATE.Length, SocketFlags.None, (X) =>
                    {
                        ClientSocket.EndSend(X);
                        StartRelay();
                    }, null);
                }
                else
                {
                    ServerSideSocket.BeginSend(ResponseBuffer, 0, ResponseBuffer.Length, SocketFlags.None, (X) =>
                    {
                        ServerSideSocket.EndSend(X);
                        StartRelay();
                    }, null);
                }
            });

        }

        private void StartRelay()
        {
            TrySocket(() =>
            {
                
                CleartBuffer(RequestBuffer);
                CleartBuffer(ResponseBuffer);
                ClientSocket.BeginReceive(RequestBuffer, 0, RequestBuffer.Length, SocketFlags.None, OnClientReceive, ClientSocket);
                ServerSideSocket.BeginReceive(ResponseBuffer, 0, ResponseBuffer.Length, SocketFlags.None, OnRemoteReceive, ServerSideSocket);
            });
        }

        private void OnRemoteReceive(IAsyncResult ar)
        {
            TrySocket(() =>
            {
                Int32 IR = ServerSideSocket.EndReceive(ar);
                ClientSocket.BeginSend(ResponseBuffer, 0, ResponseBuffer.Length, SocketFlags.None, OnClientSent, ClientSocket);
            });
        }

        private void OnClientSent(IAsyncResult ar)
        {
            TrySocket(() =>
            {
                Int32 IR = ClientSocket.EndSend(ar);
                CleartBuffer(ResponseBuffer);
                ServerSideSocket.BeginReceive(ResponseBuffer, 0, ResponseBuffer.Length, SocketFlags.None, OnRemoteReceive, ServerSideSocket);
            });
        }

        private void OnClientReceive(IAsyncResult ar)
        {
            TrySocket(() =>
            {
                Int32 IR = ClientSocket.EndReceive(ar);
                ServerSideSocket.BeginSend(RequestBuffer, 0, IR, SocketFlags.None, OnRemoteSent, ServerSideSocket);
            });
        }

        private void OnRemoteSent(IAsyncResult ar)
        {
            TrySocket(() =>
            {
                Console.WriteLine(RequestBuffer);
                Int32 IR = ServerSideSocket.EndSend(ar);
                CleartBuffer(RequestBuffer);
                ClientSocket.BeginReceive(RequestBuffer, 0, IR, SocketFlags.None, OnClientReceive, ClientSocket);
            });
        }

        public void Stop()
        {
            ServerStateChanged(false);
        }

    }
}
