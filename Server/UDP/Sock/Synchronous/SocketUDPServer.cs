using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace NetFrame.Server.UDP.Sock.Synchronous
{
    /// <summary>
    /// Socket 实现同步UDP服务器
    /// </summary>
    public class SocketUDPServer
    {
        #region Fields
        /// <summary>
        /// 服务器程序允许的最大客户端连接数
        /// </summary>
        private int _maxClient;

        /// <summary>
        /// 当前的连接的客户端数
        /// </summary>
        private int _clientCount;

        /// <summary>
        /// 服务器使用的同步socket
        /// </summary>
        private Socket _serverSock;

        /// <summary>
        /// 客户端会话列表
        /// </summary>
        private List<SocketUDPState> _clients;

        private bool disposed = false;

        /// <summary>
        /// 数据接受缓冲区
        /// </summary>
        private byte[] _recvBuffer;

        #endregion

        #region Properties

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 监听的IP地址
        /// </summary>
        public IPAddress Address { get; private set; }
        /// <summary>
        /// 监听的端口
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// 通信使用的编码
        /// </summary>
        public Encoding Encoding { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 异步Socket UDP服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        public SocketUDPServer(int listenPort)
            : this(IPAddress.Any, listenPort,1024)
        {
        }

        /// <summary>
        /// 异步Socket UDP服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        public SocketUDPServer(IPEndPoint localEP)
            : this(localEP.Address, localEP.Port,1024)
        {
        }

        /// <summary>
        /// 异步Socket UDP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        public SocketUDPServer(IPAddress localIPAddress, int listenPort, int maxClient)
        {
            this.Address = localIPAddress;
            this.Port = listenPort;
            this.Encoding = Encoding.Default;

            _maxClient = maxClient;
            _clients = new List<SocketUDPState>();
            _serverSock = new Socket(localIPAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            _recvBuffer=new byte[_serverSock.ReceiveBufferSize];
        }

        #endregion

        #region Method
        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <returns>异步TCP服务器</returns>
        public void Start()
        {
            if (!IsRunning)
            {
                IsRunning = true;
                _serverSock.Bind(new IPEndPoint(this.Address, this.Port));
                //启动一个线程监听数据
                new Thread(ReceiveData).Start();
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _serverSock.Close();
                //TODO 关闭对所有客户端的连接
                CloseAllClient();
            }
        }

        /// <summary>
        /// 同步数据接收方法
        /// </summary>
        private void ReceiveData()
        {
            int len = -1;
            EndPoint remote = null;
            while (true)
            {
                try
                {
                    len = _serverSock.ReceiveFrom(_recvBuffer, ref remote);

                    //if (!_clients.Contains(remote))
                    //{
                    //    _clients.Add(remote);
                    //}
                }
                catch (Exception)
                {
                    //TODO 异常处理操作
                    RaiseOtherException(null);
                }
            }
        }
        /// <summary>
        /// 同步发送数据
        /// </summary>
        public void Send(string msg, EndPoint clientip)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            try
            {
                _serverSock.SendTo(data, clientip);
                //数据发送完成事件
                RaiseCompletedSend(null);
            }
            catch (Exception)
            {
                //TODO 异常处理
                RaiseOtherException(null);
            }
        }

        #endregion

        #region 事件
        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event EventHandler<SocketUDPEventArgs> DataReceived;

        private void RaiseDataReceived(SocketUDPState state)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new SocketUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 数据发送完毕事件
        /// </summary>
        public event EventHandler<SocketUDPEventArgs> CompletedSend;

        /// <summary>
        /// 触发数据发送完毕的事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseCompletedSend(SocketUDPState state)
        {
            if (CompletedSend != null)
            {
                CompletedSend(this, new SocketUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 网络错误事件
        /// </summary>
        public event EventHandler<SocketUDPEventArgs> NetError;
        /// <summary>
        /// 触发网络错误事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseNetError(SocketUDPState state)
        {
            if (NetError != null)
            {
                NetError(this, new SocketUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 异常事件
        /// </summary>
        public event EventHandler<SocketUDPEventArgs> OtherException;
        /// <summary>
        /// 触发异常事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseOtherException(SocketUDPState state, string descrip)
        {
            if (OtherException != null)
            {
                OtherException(this, new SocketUDPEventArgs(descrip, state));
            }
        }
        private void RaiseOtherException(SocketUDPState state)
        {
            RaiseOtherException(state, "");
        }
        #endregion

        #region Close
        /// <summary>
        /// 关闭一个与客户端之间的会话
        /// </summary>
        /// <param name="state">需要关闭的客户端会话对象</param>
        public void Close(SocketUDPState state)
        {
            if (state != null)
            {
                _clients.Remove(state);
                _clientCount--;
                //TODO 触发关闭事件
            }
        }
        /// <summary>
        /// 关闭所有的客户端会话,与所有的客户端连接会断开
        /// </summary>
        public void CloseAllClient()
        {
            foreach (SocketUDPState client in _clients)
            {
                Close(client);
            }
            _clientCount = 0;
            _clients.Clear();
        }
        #endregion

        #region 释放
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release 
        /// both managed and unmanaged resources; <c>false</c> 
        /// to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Stop();
                        if (_serverSock != null)
                        {
                            _serverSock = null;
                        }
                    }
                    catch (SocketException)
                    {
                        //TODO
                        RaiseOtherException(null);
                    }
                }
                disposed = true;
            }
        }
        #endregion
    }
}
