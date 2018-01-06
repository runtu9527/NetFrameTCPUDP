using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetFrame.Server.TCP.Listener.Asynchronous;
using System.Net;
using System.Net.Sockets;
namespace ServerDemo
{
    public class ServerTest
    {
        public AsyncTCPServer Server = null;
       
        public ServerTest(IPEndPoint localPoint)
        {
            Server = new AsyncTCPServer(localPoint);
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
