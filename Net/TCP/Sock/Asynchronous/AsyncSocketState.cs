using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace NetFrame.Net.TCP.Sock.Asynchronous
{
    /// <summary>
    /// 异步SOCKET TCP 中用来存储客户端状态信息的类
    /// </summary>
    public class AsyncSocketState
    {
        #region 字段
        /// <summary>
        /// 接收数据缓冲区
        /// </summary>
        private byte[] _recvBuffer;

        /// <summary>
        /// 客户端发送到服务器的报文
        /// 注意:在有些情况下报文可能只是报文的片断而不完整
        /// </summary>
        private string _datagram;

        /// <summary>
        /// 客户端的Socket
        /// </summary>
        private Socket _clientSock;

        #endregion

        #region 属性

        /// <summary>
        /// 接收数据缓冲区 
        /// </summary>
        public byte[] RecvDataBuffer
        {
            get
            {
                return _recvBuffer;
            }
            set
            {
                _recvBuffer = value;
            }
        }

        /// <summary>
        /// 存取会话的报文
        /// </summary>
        public string Datagram
        {
            get
            {
                return _datagram;
            }
            set
            {
                _datagram = value;
            }
        }

        /// <summary>
        /// 获得与客户端会话关联的Socket对象
        /// </summary>
        public Socket ClientSocket
        {
            get
            {
                return _clientSock;

            }
        }


        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cliSock">会话使用的Socket连接</param>
        public AsyncSocketState(Socket cliSock)
        {
            _clientSock = cliSock;
        }

        /// <summary>
        /// 初始化数据缓冲区
        /// </summary>
        public void InitBuffer()
        {
            if (_recvBuffer == null&&_clientSock!=null)
            {
                _recvBuffer=new byte[_clientSock.ReceiveBufferSize];
            }
        }

        /// <summary>
        /// 关闭会话
        /// </summary>
        public void Close()
        {

            //关闭数据的接受和发送
            _clientSock.Shutdown(SocketShutdown.Both);

            //清理资源
            _clientSock.Close();
        }
    }
}
