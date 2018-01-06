using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetFrame.Client.TCP.AsyncSocket
{
    public class AsyncSocketCommProperty
    {
        /// <summary>
        /// 返回的数据
        /// </summary>
        protected byte[] Rcvbuffer;

        /// <summary>
        /// 存放已经接收返回的数据
        /// </summary>
        protected byte[] _bReceiveData;
        //public  AutoResetEvent myConnEvent = new AutoResetEvent(false);
        //public  AutoResetEvent ReceiveEvent = new AutoResetEvent(false);
                    //设备的IP与端口号
        protected int MaxBufLen = 1024 * 1024 * 8;
        protected byte[] ReceiveData;

        public bool IsAutoKeepAlive = false;
        public  IPEndPoint Ipep{get;set;}
     

        /// <summary>
        /// 定义网络类型，数据连接类型和网络协议
        /// </summary>
       
        protected  Socket SrvSocket{get;set;}
      

        /// <summary>
        /// 网络是否连接
        /// </summary>
        protected bool socketIsConnected = false;

        
    }
}
