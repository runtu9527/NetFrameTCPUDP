using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDemo
{
    class Program
    {
        private static ClientTest client;
        static void Main(string[] args)
        {
            client = new ClientTest();
            client.Connect("127.0.0.1", 6655);

            Console.ReadLine();
        }
    }
}
