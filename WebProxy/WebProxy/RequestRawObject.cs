using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebProxy
{

    public class ProxyRevicedInfo
    {
        Byte[] _Buffer;
        public Byte[] Buffer
        {
            get
            {
                if (_Buffer == null)
                {
                    _Buffer = new Byte[1024];
                }
                return _Buffer;
            }
        }

        public String ResponseText { get; set; }
        public Int32 RevicedLength { get; set; }

        public Socket RefenceSocket { get; set; }

        public ProxyRevicedInfo(String _ResponseText, Int32 _ReviceLength, Socket _RefenceSocket)
        {
            this.ResponseText = _ResponseText;
            this.RevicedLength = _ReviceLength;
            this.RefenceSocket = _RefenceSocket;
        }
    }
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

        public Object Tag1 { get; set; }
        public Object Tag2 { get; set; }


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
                Accept = QueryHeaderGroup.Count() >= 2 ? QueryHeaderGroup[1] : "";
                Accept_Language = QueryHeaderGroup.Count() >= 3 ? QueryHeaderGroup[2] : "";
                User_Agent = QueryHeaderGroup.Count() >= 4 ? QueryHeaderGroup[3] : "";
                Accept_Encoding = QueryHeaderGroup.Count() >= 5 ? QueryHeaderGroup[4] : "";
                Host = QueryHeaderGroup.Count() >= 6 ? QueryHeaderGroup[5] : "";
                DNT = QueryHeaderGroup.Count() >= 7 ? QueryHeaderGroup[6] : "";
                Proxy_Connection = QueryHeaderGroup.Count() >= 8 ? QueryHeaderGroup[7] : "";
            }
            else
            {
                Host = QueryHeaderGroup.Count() >= 2 ? QueryHeaderGroup[1] : "";
                Proxy_Connection = QueryHeaderGroup.Count() >= 3 ? QueryHeaderGroup[2] : "";
                User_Agent = QueryHeaderGroup.Count() >= 4 ? QueryHeaderGroup[3] : "";
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
}
