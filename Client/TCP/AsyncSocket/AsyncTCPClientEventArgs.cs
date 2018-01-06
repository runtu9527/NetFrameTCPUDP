using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NetFrame.Client.TCP.AsyncSocket
{
    public class AsyncTCPClientEventArgs:EventArgs
    {
        /// <summary>
        /// 提示信息
        /// </summary>
        public string _msg;

        /// <summary>
        /// 客户端状态封装类
        /// </summary>
        public Socket _state;

        /// <summary>
        /// 是否已经处理过了
        /// </summary>
        public bool IsHandled { get; set; }

        public AsyncTCPClientEventArgs(string msg)
        {
            this._msg = msg;
            IsHandled = false;
        }
        public AsyncTCPClientEventArgs(Socket state)
        {
            this._state = state;
            IsHandled = false;
        }
        public AsyncTCPClientEventArgs(string msg, Socket state)
        {
            this._msg = msg;
            this._state = state;
            IsHandled = false;
        }
    }
}
