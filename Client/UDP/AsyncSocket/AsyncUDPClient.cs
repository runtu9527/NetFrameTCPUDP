using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetFrame.Client.UDP.AsyncSocket
{
    public class AsyncUDPClient
    {
        public AsyncUDPClient()
        {
            //接收组播数据
            Thread t = new Thread(new ThreadStart(RecvThread));
            t.IsBackground = true;
            t.Start();

            //向组播发送数据
            IPEndPoint ipend = new IPEndPoint(IPAddress.Parse("192.168.1.107"), 26215);
            UdpClient client = new UdpClient(ipend);
            client.EnableBroadcast = false;
            client.JoinMulticastGroup(IPAddress.Parse("239.114.114.106"));
            IPEndPoint multicast = new IPEndPoint(IPAddress.Parse("239.114.114.106"), 26214);
            byte[] buf = Encoding.Default.GetBytes("<find_server>");

            client.Send(buf, buf.Length, multicast);

            //异步接收数据
            //client.BeginReceive(new AsyncCallback(ReceiveBack), client);

            //设置网络超时时间,一段时间未接收到数据时自动退出
            //client.Client.ReceiveTimeout = 150;

            //单播接收数据
            try
            {
                while (true)
                {
                    byte[] value = client.Receive(ref ipend);
                    string msg = Encoding.Default.GetString(value);
                    Console.WriteLine(msg);
                    //Console.WriteLine(ipend.Address.ToString());
                }
            }
            catch
            {
                if(client!=null)
                {
                    client.Close();
                    client = null;
                }
                return;
            }
        

        }
       


        static void RecvThread()
        {
            //绑定组播端口   
            UdpClient client = new UdpClient(26214);
            client.EnableBroadcast = false;
            client.JoinMulticastGroup(IPAddress.Parse("239.114.114.106"));
            IPEndPoint mult = null;
            while (true)
            {
                byte[] buf = client.Receive(ref mult);
                string msg = Encoding.Default.GetString(buf);
                Console.WriteLine(msg);
                //Console.WriteLine(mult.Address.ToString());
            }
        }

        static void ReceiveBack(IAsyncResult state)
        {
            try
            {
                UdpClient udpClient = (UdpClient)state.AsyncState;
                IPEndPoint endPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;

                byte[] receiveBytes = udpClient.EndReceive(state, ref endPoint);
                string value = Encoding.Default.GetString(receiveBytes);
                Console.WriteLine(value);
                //// 在这里使用异步委托来处理接收到的数组，并再次启动接收
                var ar = udpClient.BeginReceive(new AsyncCallback(ReceiveBack), udpClient);

            }
            catch (Exception e)
            {
                return;
            }

        }

    }
}
