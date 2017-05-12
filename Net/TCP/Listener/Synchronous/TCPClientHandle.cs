using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace NetFrame.Net.TCP.Listener.Synchronous
{
    /// <summary>
    /// TcpListener实现同步TCP服务器 的客户端连接处理类
    /// </summary>
    public class TCPClientHandle
    {
        private TcpClient _tcpclient;

        private BinaryReader rs;

        private BinaryWriter ws;

        /// <summary>
        /// 标识是否与客户端相连接
        /// </summary>
        private bool _is_connect;
        public bool IsConnect
        {
            get { return _is_connect; }
            set { _is_connect = value; }
        }

        /// <summary>
        /// 数据接受缓冲区
        /// </summary>
        private byte[] _recvBuffer;

        public TCPClientHandle(TcpClient client)
        {
            _tcpclient = client;
            rs = new BinaryReader(client.GetStream());
            ws = new BinaryWriter(client.GetStream());
            // NetworkStream ns = tmpTcpClient.GetStream();
            // if(ns.CanRead&&ns.CanWrite)
            _recvBuffer=new byte[client.ReceiveBufferSize];
        }

        /// <summary>
        /// 接受数据
        /// </summary>
        public void RecevieData()
        {
            int len = 0;
            while (_is_connect)
            {
                try
                {
                    len = rs.Read(_recvBuffer, 0, _recvBuffer.Length);
                }
                catch (Exception)
                {
                    break;
                }
                if (len == 0)
                {
                    //the client has disconnected from server
                    break;
                }
                //TODO 处理收到的数据
                
            }
        }
        /// <summary>
        /// 向客户端发送数据
        /// </summary>
        /// <param name="msg"></param>
        public void SendData(string msg)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            try
            {
                ws.Write(data, 0, data.Length);
                ws.Flush();
            }
            catch (Exception)
            {
                //TODO 处理异常
            }
        }

        #region 事件


        //TODO 消息发送事件
        //TODO 数据收到事件
        //TODO 异常处理事件

        #endregion

        #region 释放
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _is_connect = false;
            if (_tcpclient != null)
            {
                _tcpclient.Close();
                _tcpclient = null;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
