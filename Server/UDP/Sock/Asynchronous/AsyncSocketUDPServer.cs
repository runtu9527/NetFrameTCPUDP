using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace NetFrame.Server.UDP.Sock.Asynchronous
{
    /// <summary>
    /// SOCKET实现异步UDP服务器
    /// </summary>
    public class AsyncSocketUDPServer
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
        /// 服务器使用的同步socket
        /// </summary>
        private Socket _serverSock;

        /// <summary>
        /// 客户端会话列表
        /// </summary>
        //private List<AsyncUDPSocketState> _clients;

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
        public AsyncSocketUDPServer(int listenPort)
            : this(IPAddress.Any, listenPort,1024)
        {
        }

        /// <summary>
        /// 异步Socket UDP服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        public AsyncSocketUDPServer(IPEndPoint localEP)
            : this(localEP.Address, localEP.Port,1024)
        {
        }

        /// <summary>
        /// 异步Socket UDP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        public AsyncSocketUDPServer(IPAddress localIPAddress, int listenPort, int maxClient)
        {
            this.Address = localIPAddress;
            this.Port = listenPort;
            this.Encoding = Encoding.Default;

            _maxClient = maxClient;
            //_clients = new List<AsyncUDPSocketState>();
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
                //_serverSock.Connect(new IPEndPoint(IPAddress.Any, 0));

                AsyncSocketUDPState so = new AsyncSocketUDPState();
                so.workSocket = _serverSock;

                _serverSock.BeginReceiveFrom(so.buffer, 0, so.buffer.Length, SocketFlags.None,
                    ref so.remote, new AsyncCallback(ReceiveDataAsync), null);


                //EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                //_serverSock.BeginReceiveFrom(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None,
                //    ref sender, new AsyncCallback(ReceiveDataAsync), sender);

                //BeginReceive 和 BeginReceiveFrom的区别是什么
                /*_serverSock.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceiveDataAsync), null);*/
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

            }
        }

        /// <summary>
        /// 接收数据的方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveDataAsync(IAsyncResult ar)
        {
            AsyncSocketUDPState so = ar.AsyncState as AsyncSocketUDPState;
            //EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            int len = -1;
            try
            {
                len = _serverSock.EndReceiveFrom(ar, ref so.remote);

                //len = _serverSock.EndReceiveFrom(ar, ref sender);

                //EndReceiveFrom 和 EndReceive区别
                //len = _serverSock.EndReceive(ar);
                //TODO 处理数据

                //触发数据收到事件
                RaiseDataReceived(so);
            }
            catch (Exception)
            {
                //TODO 处理异常
                RaiseOtherException(so);
            }
            finally
            {
                if (IsRunning && _serverSock != null)
                    _serverSock.BeginReceiveFrom(so.buffer, 0, so.buffer.Length, SocketFlags.None,
                ref so.remote, new AsyncCallback(ReceiveDataAsync), so);
            }
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="remote"></param>
        public void Send(string msg,EndPoint remote)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            try
            {
                RaisePrepareSend(null);
                _serverSock.BeginSendTo(data, 0, data.Length, SocketFlags.None, remote, new AsyncCallback(SendDataEnd), _serverSock);
            }
            catch (Exception)
            {
                //TODO 异常处理
                RaiseOtherException(null);
            }
        }

        private void SendDataEnd(IAsyncResult ar)
        {
            ((Socket)ar.AsyncState).EndSendTo(ar);
            RaiseCompletedSend(null);
        }

        #endregion

        #region 事件
        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event EventHandler<AsyncSocketUDPEventArgs> DataReceived;

        private void RaiseDataReceived(AsyncSocketUDPState state)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new AsyncSocketUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 发送数据前的事件
        /// </summary>
        public event EventHandler<AsyncSocketUDPEventArgs> PrepareSend;

        /// <summary>
        /// 触发发送数据前的事件
        /// </summary>
        /// <param name="state"></param>
        private void RaisePrepareSend(AsyncSocketUDPState state)
        {
            if (PrepareSend != null)
            {
                PrepareSend(this, new AsyncSocketUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 数据发送完毕事件
        /// </summary>
        public event EventHandler<AsyncSocketUDPEventArgs> CompletedSend;

        /// <summary>
        /// 触发数据发送完毕的事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseCompletedSend(AsyncSocketUDPState state)
        {
            if (CompletedSend != null)
            {
                CompletedSend(this, new AsyncSocketUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 网络错误事件
        /// </summary>
        public event EventHandler<AsyncSocketUDPEventArgs> NetError;
        /// <summary>
        /// 触发网络错误事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseNetError(AsyncSocketUDPState state)
        {
            if (NetError != null)
            {
                NetError(this, new AsyncSocketUDPEventArgs(state));
            }
        }

        /// <summary>
        /// 异常事件
        /// </summary>
        public event EventHandler<AsyncSocketUDPEventArgs> OtherException;
        /// <summary>
        /// 触发异常事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseOtherException(AsyncSocketUDPState state, string descrip)
        {
            if (OtherException != null)
            {
                OtherException(this, new AsyncSocketUDPEventArgs(descrip, state));
            }
        }
        private void RaiseOtherException(AsyncSocketUDPState state)
        {
            RaiseOtherException(state, "");
        }
        #endregion

        #region Close
        /// <summary>
        /// 关闭一个与客户端之间的会话
        /// </summary>
        /// <param name="state">需要关闭的客户端会话对象</param>
        public void Close(AsyncSocketUDPState state)
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
