using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFrame.Server.UDP.Listener.Asynchronous
{
    /// <summary>
    /// UdpClient 异步UDP服务器事件参数类
    /// </summary>
    public class AsyncUDPEventArgs : EventArgs
    {
        /// <summary>
        /// 提示信息
        /// </summary>
        public string _msg;

        /// <summary>
        /// 客户端状态封装类
        /// </summary>
        public AsyncUDPState _state;

        /// <summary>
        /// 是否已经处理过了
        /// </summary>
        public bool IsHandled { get; set; }

        public AsyncUDPEventArgs(string msg)
        {
            this._msg = msg;
            IsHandled = false;
        }
        public AsyncUDPEventArgs(AsyncUDPState state)
        {
            this._state = state;
            IsHandled = false;
        }
        public AsyncUDPEventArgs(string msg, AsyncUDPState state)
        {
            this._msg = msg;
            this._state = state;
            IsHandled = false;
        }
    }
}
