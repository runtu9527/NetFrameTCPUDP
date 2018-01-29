using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace NetFrame.TCP.Server.Sock.IOCP
{
    /// <summary>
    /// 用户对象
    /// 注意事项:一个Socket的Send和Receive最好分别对应一个SocketAsyncEventArgs
    /// </summary>
    public class AsyncUserToken
    {
        #region 字段
        /// <summary>
        /// 接收数据的SocketAsyncEventArgs
        /// </summary>
        private SocketAsyncEventArgs _receiveEventArgs;

        /// <summary>
        /// 发送数据的SocketAsyncEventArgs
        /// </summary>
        private SocketAsyncEventArgs _sendEventArgs;

        /// <summary>
        /// 接收数据的缓冲区
        /// </summary>
        private byte[] _asyncReceiveBuffer;

        /// <summary>
        /// 发送数据的缓冲区
        /// </summary>
        private byte[] _asyncSendBuffer;
        /// <summary>
        /// 动态的接收缓冲区
        /// </summary>
        private DynamicBufferManager _receiveBuffer;

        /// <summary>
        /// 动态的发送缓冲区
        /// </summary>
        private DynamicBufferManager _sendBuffer;

        /// <summary>
        /// 连接的Socket对象
        /// </summary>
        private Socket _connectSocket;

        #endregion

        #region 属性

        public SocketAsyncEventArgs ReceiveEventArgs
        {
            get { return _receiveEventArgs; }
            set { _receiveEventArgs = value; }
        }

        public SocketAsyncEventArgs SendEventArgs
        {
            get { return _sendEventArgs; }
            set { _sendEventArgs = value; }
        }

        public Socket ConnectSocket
        {
            get { return _connectSocket; }
            set { _connectSocket = value; }
        }

        public DynamicBufferManager ReceiveBuffer
        {
            get { return _receiveBuffer; }
            set { _receiveBuffer = value; }
        }

        public DynamicBufferManager SendBuffer
        {
            get { return _sendBuffer; }
            set { _sendBuffer = value; }
        }
        #endregion

        public AsyncUserToken(int ReceiveBufferSize)
        {
            _connectSocket = null;
            _receiveEventArgs = new SocketAsyncEventArgs();
            _receiveEventArgs.UserToken = this;

            _asyncReceiveBuffer = new byte[ReceiveBufferSize];
            _receiveEventArgs.SetBuffer(_asyncReceiveBuffer, 0, _asyncReceiveBuffer.Length);//设置接收缓冲区

            _sendEventArgs = new SocketAsyncEventArgs();
            _sendEventArgs.UserToken = this;
            _asyncSendBuffer = new byte[ReceiveBufferSize];
            _sendEventArgs.SetBuffer(_asyncSendBuffer, 0, _asyncSendBuffer.Length);//设置发送缓冲区
            _receiveBuffer = new DynamicBufferManager(1024 * 4);
            _sendBuffer = new DynamicBufferManager(1024 * 4);
        }
    }
}
