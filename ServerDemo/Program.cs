using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace ServerDemo
{
    class Program
    {
        private static ServerTest server;
        private static IPEndPoint localPoint;
        private static TcpClient socket;
        private static System.Timers.Timer timer;
        static void Main(string[] args)
        {
            localPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 6655);
            server = new ServerTest(localPoint);
            server.Server.ClientConnected += Server_ClientConnected;
            server.Server.ClientDisconnected += Server_ClientDisconnected;
            server.Server.DataReceived += Server_DataReceived;
            server.Server.CompletedSend += Server_CompletedSend;
            server.Start();

            Console.ReadLine();
        }

        static void Server_CompletedSend(object sender, NetFrame.Server.TCP.Listener.Asynchronous.AsyncEventArgs e)
        {
            
        }

        static void Server_DataReceived(object sender, NetFrame.Server.TCP.Listener.Asynchronous.AsyncEventArgs e)
        {
            
        }

        static void Server_ClientDisconnected(object sender, NetFrame.Server.TCP.Listener.Asynchronous.AsyncEventArgs e)
        {
            socket = null;
        }

        static void Server_ClientConnected(object sender, NetFrame.Server.TCP.Listener.Asynchronous.AsyncEventArgs e)
        {
            Console.WriteLine("客户端连接成功" + e._state.TcpClient.Client.RemoteEndPoint.ToString());
            socket = e._state.TcpClient;

            timer = new System.Timers.Timer();
            timer.Elapsed += timer_Elapsed;
            timer.Interval = 80;
            timer.Enabled = true;

        }

        private static int num = 0;

        static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            num++;
            byte[] n = intToBytes2(num);
            byte[] sendbyte=n.Concat(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29 ,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 }).ToArray();
            server.Server.Send(socket, sendbyte);
        }

        public static byte[] intToBytes2(int value)
        {
            byte[] src = new byte[4];
            src[0] = (byte)((value >> 24) & 0xFF);
            src[1] = (byte)((value >> 16) & 0xFF);
            src[2] = (byte)((value >> 8) & 0xFF);
            src[3] = (byte)(value & 0xFF);
            return src;
        }  

    }
}
