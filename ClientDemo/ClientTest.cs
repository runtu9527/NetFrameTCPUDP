using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using NetFrame.TCP.Client.AsyncSocket;

namespace ClientDemo
{
    public class ClientTest
    {
        public AsyncSocketComm Client = null;
        public ClientTest()
        {
            Client = new AsyncSocketComm();
            Client.ClientConnected += Client_ClientConnected;
            Client.DataReceived += Client_DataReceived;
            Client.DataSend += Client_DataSend;
        }

        void Client_DataSend(object sender, AsyncTCPClientEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void Client_DataReceived(object sender, AsyncTCPClientEventArgs e)
        {
            byte[] data=sender as byte[];

            Console.WriteLine(data.Length);
     
            //System.Threading.Thread.Sleep(200);
        }

        void Client_ClientConnected(object sender, AsyncTCPClientEventArgs e)
        {
            Console.WriteLine("客户端连接成功," + "客户端地址：" + e._state.LocalEndPoint.ToString() + "  服务器地址：" + e._state.RemoteEndPoint.ToString());
        }

        public void Connect(string ip,int port)
        {
            Client.BuildServerSocket(ip, port);
            Client.MaxBufLen = 48;
        }
    }
}
