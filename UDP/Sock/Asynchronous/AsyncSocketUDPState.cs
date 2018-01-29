using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace NetFrame.UDP.Sock.Asynchronous
{
    public class AsyncSocketUDPState
    {
        // Client   socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();

        public EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
    }
}
