using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFrame.Server.UDP.Sock.Synchronous
{
    /// <summary>
    /// Socket实现同步UDP服务器
    /// </summary>
    public class SocketUDPEventArgs:EventArgs
    {
        /// <summary>
        /// 提示信息
        /// </summary>
        public string _msg;

        /// <summary>
        /// 客户端状态封装类
        /// </summary>
        public SocketUDPState _state;

        /// <summary>
        /// 是否已经处理过了
        /// </summary>
        public bool IsHandled { get; set; }

        public SocketUDPEventArgs(string msg)
        {
            this._msg = msg;
            IsHandled = false;
        }
        public SocketUDPEventArgs(SocketUDPState state)
        {
            this._state = state;
            IsHandled = false;
        }
        public SocketUDPEventArgs(string msg, SocketUDPState state)
        {
            this._msg = msg;
            this._state = state;
            IsHandled = false;
        }
    }
}
