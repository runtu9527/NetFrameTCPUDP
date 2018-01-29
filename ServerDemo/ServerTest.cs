using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetFrame.TCP.Server.Sock.Synchronous;
using System.Net;
using System.Net.Sockets;
namespace ServerDemo
{
    public class ServerTest
    {
        public SocketTCPServer Server = null;
       
        public ServerTest(IPEndPoint localPoint)
        {
            Server = new SocketTCPServer(localPoint);
        }

        public void Start()
        {
            if (Server != null)
            {
                Server.Start();
            }
        }
    }
}
