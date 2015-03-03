using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebProxy
{
    public class ProxyServer2
    {
        private Socket clientSocket;
        private byte[] read = new byte[1024];
        private byte[] buffer = null;
        private Encoding ASCII = Encoding.ASCII;
        private String HTTP_VERSION = "HTTP/1.0";
        private String CRLF = "\r\n";
        private byte[] recvBytes = new byte[4096];
        public void setSocket(Socket socket)
        {
            this.clientSocket = socket;
        }

        public void run()
        {
            String clientMessage = "";
            String sURL = "";

            String m = readMessage(read, clientSocket, clientMessage);

            int bytes = m.Length;

            if (bytes != 0)
            {
                RequestRawObject RRO = new RequestRawObject(m);
               

                //sURL = m.Trim();  
                //sURL = sURL.Replace("\0", "");  
                //String ipaddresss = sURL;  
                try
                {
                    IPHostEntry IPHost = Dns.GetHostEntry(RRO.RequestURL.Host); //Dns.Resolve(sURL);  
                    Console.WriteLine("Request resolved: ", IPHost.HostName);
                    String[] aliases = IPHost.Aliases;
                    IPAddress[] address = IPHost.AddressList;
                    //IPAddress address = IPAddress.Parse(ipaddresss);  
                    Console.WriteLine(address[0]);
                    //Console.WriteLine(address);  
                    IPEndPoint sEndpoint = new IPEndPoint(address[0], 80);
                    //IPEndPoint sEndpoint = new IPEndPoint(address, 80);
                    Socket IPsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPsocket.Connect(sEndpoint);
                    if (IPsocket.Connected)
                    {
                        Console.WriteLine("Socket connect OK");

                        String GET = clientMessage;
                        Byte[] ByteGet = ASCII.GetBytes(GET);
                        IPsocket.Send(ByteGet, ByteGet.Length, 0);

                        Int32 rBytes = IPsocket.Receive(recvBytes, recvBytes.Length, 0);
                        Console.WriteLine("Recieved {0}", +rBytes);

                        String strRetPage = "";
                        strRetPage = strRetPage + ASCII.GetString(recvBytes, 0, rBytes);
                        while (rBytes > 0)
                        {
                            rBytes = IPsocket.Receive(recvBytes, recvBytes.Length, 0);
                            strRetPage = strRetPage + ASCII.GetString(recvBytes, 0, rBytes);
                        }

                        IPsocket.Shutdown(SocketShutdown.Both);
                        IPsocket.Close();
                        sendMessage(clientSocket, strRetPage);
                    }
                    else
                    {
                        Console.WriteLine("Socket connect Error");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }


        }

        private string readMessage(byte[] byteArray, Socket s, String clientMessage)
        {
            int bytes = s.Receive(byteArray, 1024, 0);
            String messageFromClient = Encoding.ASCII.GetString(byteArray);
            return messageFromClient;
            //clientMessage = messageFromClient;  

            //return bytes;  
        }

        // bool  
        private void sendMessage(Socket s, String message)
        {
            this.buffer = new Byte[message.Length + 1];
            int length = ASCII.GetBytes(message, 0, message.Length, buffer, 0);
            s.Send(buffer, length, 0);
        }



    }
}
