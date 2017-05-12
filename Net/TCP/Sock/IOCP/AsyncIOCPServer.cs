using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace NetFrame.Net.TCP.Sock.IOCP
{
    /// <summary>
    /// Socket实现IOCP异步服务器
    /// </summary>
    public class AsyncIOCPServer
    {
        #region Fields
        /// <summary>
        /// 服务器程序允许的最大客户端连接数
        /// </summary>
        private int _maxClient;

        /// <summary>
        /// 监听Socket，用于接受客户端的连接请求
        /// </summary>
        private Socket _serverSock;

        /// <summary>
        /// 当前的连接的客户端数
        /// </summary>
        private int _clientCount;

        /// <summary>
        /// 用于每个I/O Socket操作的缓冲区大小 默认1024
        /// </summary>
        private int _bufferSize = 1024;

        /// <summary>
        /// 信号量
        /// </summary>
        Semaphore _maxAcceptedClients;

        /// <summary>
        /// 对象池
        /// </summary>
        AsyncUserTokenPool _userTokenPool;

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

        public int BufferSize
        {
            get { return _bufferSize; }
            set { _bufferSize = value; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 异步IOCP SOCKET服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大的客户端数量</param>
        public AsyncIOCPServer(int listenPort, int maxClient)
            : this(IPAddress.Any, listenPort, maxClient)
        {
        }

        /// <summary>
        /// 异步Socket TCP服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        /// <param name="maxClient">最大客户端数量</param>
        public AsyncIOCPServer(IPEndPoint localEP, int maxClient)
            : this(localEP.Address, localEP.Port, maxClient)
        {
        }

        /// <summary>
        /// 异步Socket TCP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        public AsyncIOCPServer(IPAddress localIPAddress, int listenPort, int maxClient)
        {
            this.Address = localIPAddress;
            this.Port = listenPort;
            this.Encoding = Encoding.Default;

            _maxClient = maxClient;

            _serverSock = new Socket(localIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


            _userTokenPool = new AsyncUserTokenPool(_maxClient);

            _maxAcceptedClients = new Semaphore(_maxClient, _maxClient);
        }

        #endregion

        #region Method

        /// <summary>
        /// 初始化函数
        /// </summary>
        public void Init()
        {
            AsyncUserToken userToken;
            for (int i = 0; i < _maxClient; i++)
            {
                userToken = new AsyncUserToken(_bufferSize);
                userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                userToken.SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                _userTokenPool.Push(userToken);
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                Init();
                IsRunning = true;
                IPEndPoint localEndPoint = new IPEndPoint(Address, Port);
                // 创建监听socket
                _serverSock = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // 配置监听socket为 dual-mode (IPv4 & IPv6) 
                    // 27 is equivalent to IPV6_V6ONLY socket option in the winsock snippet below,
                    _serverSock.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _serverSock.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
                }
                else
                {
                    _serverSock.Bind(localEndPoint);
                }
                // 开始监听
                _serverSock.Listen(this._maxClient);
                // 在监听Socket上投递一个接受请求。
                StartAccept(null);
            }
        }

        /// <summary>
        /// 停止服务
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
        /// 从客户端开始接受一个连接操作
        /// </summary>
        private void StartAccept(SocketAsyncEventArgs asyniar)
        {
            if (asyniar == null)
            {
                asyniar = new SocketAsyncEventArgs();
                asyniar.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
            {
                //socket must be cleared since the context object is being reused
                asyniar.AcceptSocket = null;
            }
            _maxAcceptedClients.WaitOne();
            if (!_serverSock.AcceptAsync(asyniar))
            {
                ProcessAccept(asyniar);
                //如果I/O挂起等待异步则触发AcceptAsyn_Asyn_Completed事件
                //此时I/O操作同步完成，不会触发Asyn_Completed事件，所以指定BeginAccept()方法
            }
        }

        /// <summary>
        /// accept 操作完成时回调函数
        /// </summary>
        /// <param name="sender">Object who raised the event.</param>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// 监听Socket接受处理
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket sock = e.AcceptSocket;//和客户端关联的socket
                if (sock.Connected)
                {
                    try
                    {
                        Interlocked.Increment(ref _clientCount);//原子操作加1
                        AsyncUserToken userToken = _userTokenPool.Pop();
                        userToken.ConnectSocket = sock;

                        //Log4Debug(String.Format("客户 {0} 连入, 共有 {1} 个连接。", sock.RemoteEndPoint.ToString(), _clientCount));

                        if (!sock.ReceiveAsync(userToken.ReceiveEventArgs))//投递接收请求
                        {
                            ProcessReceive(userToken.ReceiveEventArgs);
                        }
                    }
                    catch (SocketException ex)
                    {
                        //Log4Debug(String.Format("接收客户 {0} 数据出错, 异常信息： {1} 。", sock.RemoteEndPoint, ex.ToString()));
                        //TODO 异常处理
                    }
                    //投递下一个接受请求
                    StartAccept(e);
                }
            }
        }

        /// <summary>
        /// 异步的发送数据
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        public void Send(SocketAsyncEventArgs e, byte[] data)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            userToken.SendBuffer.WriteBuffer(data, 0, data.Length);//写入要发送的数据
            if (userToken.SendEventArgs.SocketError == SocketError.Success)
            {
                if (userToken.ConnectSocket.Connected)
                {
                    //设置发送数据
                    //userToken.SendEventArgs.SetBuffer(userToken.SendBuffer.Buffer,0,userToken.SendBuffer.DataCount);

                    Array.Copy(data, 0, e.Buffer, 0, data.Length);//设置发送数据

                    if (!userToken.ConnectSocket.SendAsync(userToken.SendEventArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        // 同步发送时处理发送完成事件
                        ProcessSend(userToken.SendEventArgs);
                    }
                    userToken.SendBuffer.Clear();
                }
                else
                {
                    CloseClientSocket(userToken);
                }
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }

        /// <summary>
        /// 同步的使用socket发送数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="timeout"></param>
        public void Send(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            socket.SendTimeout = 0;
            int startTickCount = Environment.TickCount;
            int sent = 0; // how many bytes is already sent
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                {
                    //throw new Exception("Timeout.");
                }
                try
                {
                    sent += socket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                    ex.SocketErrorCode == SocketError.IOPending ||
                    ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                    {
                        throw ex; // any serious error occurr
                    }
                }
            } while (sent < size);
        }


        /// <summary>
        /// 发送完成时处理函数
        /// </summary>
        /// <param name="e">与发送完成操作相关联的SocketAsyncEventArg对象</param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            if (userToken.SendEventArgs.SocketError == SocketError.Success)
            {
                //TODO
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }

        /// <summary>
        ///接收完成时处理函数
        /// </summary>
        /// <param name="e">与接收完成操作相关联的SocketAsyncEventArg对象</param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            if (userToken.ReceiveEventArgs.BytesTransferred > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                Socket sock = userToken.ConnectSocket;
                //判断所有需接收的数据是否已经完成
                if (sock.Available == 0)
                {
                    //把收到的数据写入到缓存区里面
                    userToken.ReceiveBuffer.WriteBuffer(e.Buffer, e.Offset, e.BytesTransferred);
                    //TODO 处理数据

                    string info = Encoding.Default.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                    //Log4Debug(String.Format("收到 {0} 数据为 {1}", sock.RemoteEndPoint.ToString(), info));

                    Send(userToken.SendEventArgs, e.Buffer);
                }

                if (!sock.ReceiveAsync(userToken.ReceiveEventArgs))//为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                {
                    //同步接收时处理接收完成事件
                    ProcessReceive(userToken.ReceiveEventArgs);
                }
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }

        /// <summary>
        /// 当Socket上的发送或接收请求被完成时，调用此函数
        /// </summary>
        /// <param name="sender">激发事件的对象</param>
        /// <param name="e">与发送或接收完成操作相关联的SocketAsyncEventArg对象</param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            // Determine which type of operation just completed and call the associated handler.
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            lock (userToken)
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Accept:
                        ProcessAccept(e);
                        break;
                    case SocketAsyncOperation.Receive:
                        ProcessReceive(e);
                        break;
                    //case SocketAsyncOperation.Send:
                    //    ProcessSend(e);
                    //    break;

                }
            }
        }
        #endregion

        #region Close
        /// <summary>
        /// 关闭socket连接
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed send/receive operation.</param>
        private void CloseClientSocket(AsyncUserToken userToken)
        {
            if (userToken.ConnectSocket == null)
                return;

            //Log4Debug(String.Format("客户 {0} 断开连接!", userToken.ConnectSocket.RemoteEndPoint.ToString()));
            try
            {
                userToken.ConnectSocket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
                // Throw if client has closed, so it is not necessary to catch.
            }
            finally
            {
                userToken.ConnectSocket.Close();
            }
            Interlocked.Decrement(ref _clientCount);
            userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源
            _maxAcceptedClients.Release();
            _userTokenPool.Push(userToken);

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
                    catch (SocketException ex)
                    {
                        //TODO 事件
                    }
                }
                disposed = true;
            }
        }
        #endregion
    }
}
