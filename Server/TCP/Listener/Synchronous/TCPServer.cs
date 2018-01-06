using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace NetFrame.Server.TCP.Listener.Synchronous
{
    /// <summary>
    /// TcpListener实现同步TCP服务器
    /// </summary>
    public class TCPServer
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
        /// 服务器使用的异步TcpListener
        /// </summary>
        private TcpListener _listener;

        /// <summary>
        /// 客户端会话列表
        /// </summary>
        private List<TCPClientHandle> _clients;

        private bool disposed = false;

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

        #region 构造器
        /// <summary>
        /// 同步TCP服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        public TCPServer(int listenPort)
            : this(IPAddress.Any, listenPort, 1024)
        {
        }

        /// <summary>
        /// 同步TCP服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        public TCPServer(IPEndPoint localEP)
            : this(localEP.Address, localEP.Port, 1024)
        {
        }

        /// <summary>
        /// 同步TCP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        public TCPServer(IPAddress localIPAddress, int listenPort, int maxClient)
        {
            this.Address = localIPAddress;
            this.Port = listenPort;
            this.Encoding = Encoding.Default;

            _maxClient = maxClient;
            _clients = new List<TCPClientHandle>();
            _listener = new TcpListener(new IPEndPoint(this.Address, this.Port));
        }

        #endregion

        #region Method
        /// <summary>
        /// 启动服务器
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                IsRunning = true;
                _listener.Start();
                Thread thread = new Thread(Accept);
                thread.Start();
            }
        }
        /// <summary>
        /// 开始进行监听
        /// </summary>
        private void Accept()
        {
            TCPClientHandle handle;
            while (IsRunning)
            {
                TcpClient client = _listener.AcceptTcpClient();
                if (_clientCount >= _maxClient)
                {
                    //TODO 触发事件
                }
                else
                {
                    handle = new TCPClientHandle(client);
                    _clientCount++;
                    _clients.Add(handle);

                    //TODO 创建一个处理客户端的线程并启动
                    //使用线程池来操作
                    new Thread(new ThreadStart(handle.RecevieData)).Start();
                }
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
                _listener.Stop();
                //TODO 关闭对所有客户端的连接
            }
        }
        /// <summary>
        /// 发送函数
        /// </summary>
        public void Send(string msg, TcpClient client)
        {
            //TODO
        }

        #endregion

        #region 事件

        /// <summary>
        /// 与客户端的连接已建立事件
        /// </summary>
        public event EventHandler<TCPEventArgs> ClientConnected;
        /// <summary>
        /// 与客户端的连接已断开事件
        /// </summary>
        public event EventHandler<TCPEventArgs> ClientDisconnected;

        /// <summary>
        /// 触发客户端连接事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseClientConnected(TCPClientHandle handle)
        {
            if (ClientConnected != null)
            {
                ClientConnected(this, new TCPEventArgs(handle));
            }
        }
        /// <summary>
        /// 触发客户端连接断开事件
        /// </summary>
        /// <param name="client"></param>
        private void RaiseClientDisconnected(Socket client)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, new TCPEventArgs("连接断开"));
            }
        }

        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event EventHandler<TCPEventArgs> DataReceived;

        private void RaiseDataReceived(TCPClientHandle handle)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new TCPEventArgs(handle));
            }
        }

        /// <summary>
        /// 数据发送事件
        /// </summary>
        public event EventHandler<TCPEventArgs> CompletedSend;

        /// <summary>
        /// 触发数据发送事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseCompletedSend(TCPClientHandle handle)
        {
            if (CompletedSend != null)
            {
                CompletedSend(this, new TCPEventArgs(handle));
            }
        }


        /// <summary>
        /// 网络错误事件
        /// </summary>
        public event EventHandler<TCPEventArgs> NetError;
        /// <summary>
        /// 触发网络错误事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseNetError(TCPClientHandle handle)
        {
            if (NetError != null)
            {
                NetError(this, new TCPEventArgs(handle));
            }
        }

        /// <summary>
        /// 异常事件
        /// </summary>
        public event EventHandler<TCPEventArgs> OtherException;
        /// <summary>
        /// 触发异常事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseOtherException(TCPClientHandle handle, string descrip)
        {
            if (OtherException != null)
            {
                OtherException(this, new TCPEventArgs(descrip, handle));
            }
        }
        private void RaiseOtherException(TCPClientHandle handle)
        {
            RaiseOtherException(handle, "");
        }

        #endregion

        #region Close

        /// <summary>
        /// 关闭一个与客户端之间的会话
        /// </summary>
        /// <param name="handle">需要关闭的客户端会话对象</param>
        public void Close(TCPClientHandle handle)
        {
            if (handle != null)
            {
                _clients.Remove(handle);
                handle.Dispose();
                _clientCount--;
                //TODO 触发关闭事件

            }
        }
        /// <summary>
        /// 关闭所有的客户端会话,与所有的客户端连接会断开
        /// </summary>
        public void CloseAllClient()
        {
            foreach (TCPClientHandle handle in _clients)
            {
                Close(handle);
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
                        if (_listener != null)
                        {
                            _listener = null;
                        }
                    }
                    catch (SocketException)
                    {
                        //TODO 异常
                    }
                }
                disposed = true;
            }
        }
        #endregion
    }
}
