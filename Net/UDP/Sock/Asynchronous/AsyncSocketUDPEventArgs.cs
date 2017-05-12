using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFrame.Net.UDP.Sock.Asynchronous
{
    /// <summary>
    /// SOCKET 异步UDP 事件类
    /// </summary>
    public class AsyncSocketUDPEventArgs : EventArgs
    {
        /// <summary>
        /// 提示信息
        /// </summary>
        public string _msg;

        /// <summary>
        /// 客户端状态封装类
        /// </summary>
        public AsyncSocketUDPState _state;

        /// <summary>
        /// 是否已经处理过了
        /// </summary>
        public bool IsHandled { get; set; }

        public AsyncSocketUDPEventArgs(string msg)
        {
            this._msg = msg;
            IsHandled = false;
        }
        public AsyncSocketUDPEventArgs(AsyncSocketUDPState state)
        {
            this._state = state;
            IsHandled = false;
        }
        public AsyncSocketUDPEventArgs(string msg, AsyncSocketUDPState state)
        {
            this._msg = msg;
            this._state = state;
            IsHandled = false;
        }
    }
}
