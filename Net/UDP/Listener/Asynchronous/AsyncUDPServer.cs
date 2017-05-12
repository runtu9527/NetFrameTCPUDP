using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace NetFrame.Net.UDP.Listener.Asynchronous
{
    /// <summary>
    /// UdpClient 实现异步UDP服务器
    /// </summary>
    public class AsyncUDPServer
    {
        #region Fields
        /// <summary>
        /// 服务器程序允许的最大客户端连接数
        /// </summary>
        private int _maxClient;

        /// <summary>
        /// 当前的连接的客户端数
        /// </summary>
        //private int _clientCount;

        /// <summary>
        /// 服务器使用的异步UdpClient
        /// </summary>
        private UdpClient _server;

        /// <summary>
        /// 客户端会话列表
        /// </summary>
        //private List<AsyncUDPState> _clients;

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
        /// 异步UdpClient UDP服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        public AsyncUDPServer(int listenPort)
            : this(IPAddress.Any, listenPort,1024)
        {
        }

        /// <summary>
        /// 异步UdpClient UDP服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        public AsyncUDPServer(IPEndPoint localEP)
            : this(localEP.Address, localEP.Port,1024)
        {
        }

        /// <summary>
        /// 异步UdpClient UDP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        public AsyncUDPServer(IPAddress localIPAddress, int listenPort, int maxClient)
        {
            this.Address = localIPAddress;
            this.Port = listenPort;
            this.Encoding = Encoding.Default;

            _maxClient = maxClient;
            //_clients = new List<AsyncUDPSocketState>();
            _server = new UdpClient(new IPEndPoint(this.Address, this.Port));

            _recvBuffer=new byte[_server.Client.ReceiveBufferSize];
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
                _server.EnableBroadcast = true;
                _server.BeginReceive(ReceiveDataAsync, null);
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
                _server.Close();
                //TODO 关闭对所有客户端的连接

            }
        }

        /// <summary>
        /// 接收数据的方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveDataAsync(IAsyncResult ar)
        {
            IPEndPoint remote=null;
            byte[] buffer = null;
            try
            {
                buffer = _server.EndReceive(ar, ref remote);

                //触发数据收到事件
                RaiseDataReceived(null);
            }
            catch (Exception)
            {
                //TODO 处理异常
                RaiseOtherException(null);
            }
            finally
            {
                if (IsRunning && _server != null)
                    _server.BeginReceive(ReceiveDataAsync, null);
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="remote"></param>
        public void Send(string msg, IPEndPoint remote)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            try
            {
                RaisePrepareSend(null);
                _server.BeginSend(data, data.Length, new AsyncCallback(SendCallback), null);
            }
            catch (Exception)
            {
                //TODO 异常处理
                RaiseOtherException(null);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                try
                {
                    _server.EndSend(ar);
                    //消息发送完毕事件
                    RaiseCompletedSend(null);
                }
                catch (Exception)
                {
                    //TODO 数据发送失败事件
                    RaiseOtherException(null);
                }
            }

        }
        #endregion

        #region 事件
        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event EventHandler<AsyncUDPEventArgs> DataReceived;

        private void RaiseDataReceived(AsyncUDPState state)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new AsyncUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 发送数据前的事件
        /// </summary>
        public event EventHandler<AsyncUDPEventArgs> PrepareSend;

        /// <summary>
        /// 触发发送数据前的事件
        /// </summary>
        /// <param name="state"></param>
        private void RaisePrepareSend(AsyncUDPState state)
        {
            if (PrepareSend != null)
            {
                PrepareSend(this, new AsyncUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 数据发送完毕事件
        /// </summary>
        public event EventHandler<AsyncUDPEventArgs> CompletedSend;

        /// <summary>
        /// 触发数据发送完毕的事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseCompletedSend(AsyncUDPState state)
        {
            if (CompletedSend != null)
            {
                CompletedSend(this, new AsyncUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 网络错误事件
        /// </summary>
        public event EventHandler<AsyncUDPEventArgs> NetError;
        /// <summary>
        /// 触发网络错误事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseNetError(AsyncUDPState state)
        {
            if (NetError != null)
            {
                NetError(this, new AsyncUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 异常事件
        /// </summary>
        public event EventHandler<AsyncUDPEventArgs> OtherException;
        /// <summary>
        /// 触发异常事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseOtherException(AsyncUDPState state, string descrip)
        {
            if (OtherException != null)
            {
                OtherException(this, new AsyncUDPEventArgs(descrip, state));
            }
        }
        private void RaiseOtherException(AsyncUDPState state)
        {
            RaiseOtherException(state, "");
        }
        #endregion

        #region Close
        /// <summary>
        /// 关闭一个与客户端之间的会话
        /// </summary>
        /// <param name="state">需要关闭的客户端会话对象</param>
        public void Close(AsyncUDPState state)
        {
            if (state != null)
            {
                //_clients.Remove(state);
                //_clientCount--;
                //TODO 触发关闭事件
            }
        }
        /// <summary>
        /// 关闭所有的客户端会话,与所有的客户端连接会断开
        /// </summary>
        public void CloseAllClient()
        {
            //foreach (AsyncUDPSocketState client in _clients)
            //{
            //    Close(client);
            //}
            //_clientCount = 0;
            //_clients.Clear();
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
                        if (_server != null)
                        {
                            _server = null;
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
