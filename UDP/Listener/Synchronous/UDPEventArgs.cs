using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFrame.UDP.Listener.Synchronous
{
    /// <summary>
    /// UdpClient 同步UDP服务器事件类
    /// </summary>
    public class UDPEventArgs : EventArgs
    {
        /// <summary>
        /// 提示信息
        /// </summary>
        public string _msg;

        /// <summary>
        /// 客户端状态封装类
        /// </summary>
        public UDPState _state;

        /// <summary>
        /// 是否已经处理过了
        /// </summary>
        public bool IsHandled { get; set; }

        public UDPEventArgs(string msg)
        {
            this._msg = msg;
            IsHandled = false;
        }
        public UDPEventArgs(UDPState state)
        {
            this._state = state;
            IsHandled = false;
        }
        public UDPEventArgs(string msg, UDPState state)
        {
            this._msg = msg;
            this._state = state;
            IsHandled = false;
        }
    }
}
