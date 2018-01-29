using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetFrame.TCP.Client.AsyncSocket
{
    /// <summary>
    /// 所有的数据，都通过此类接口发送到终端服务器中
    /// 异步通信
    /// </summary>
    public class AsyncSocketComm : AsyncSocketCommFunc
    {
        public event EventHandler<AsyncTCPClientEventArgs> DataReceived;
        public event EventHandler<AsyncTCPClientEventArgs> DataSend;
        public event EventHandler<AsyncTCPClientEventArgs> ClientConnected;
        public event EventHandler<AsyncTCPClientEventArgs> SocketException;
        public event EventHandler<AsyncTCPClientEventArgs> OtherException;

        #region [构建Socket]
        /// <summary>
        ///  构建Socket
        /// </summary>
        /// <param name="strIP"></param>
        /// <param name="iPort"></param>
        public AsyncSocketComm(bool isAutoKeepAlive = false)
        {
            IsAutoKeepAlive = isAutoKeepAlive;
        }

        #endregion

        /// <summary>
        /// 创建IPEndPoint
        /// </summary>
        /// <param name="strIP"></param>
        /// <param name="iPort"></param>
        /// <returns></returns>
        private IPEndPoint BuildIPEndPoint(string strIP, int iPort)
        {
            //从配置文件获取ip地址和端口号
            Ipep = new IPEndPoint(IPAddress.Parse(strIP), iPort);
            return Ipep;
        }

        #region[异步接收数据]

        //构建Socket
        public void BuildServerSocket(string strIP, int iPort)
        {
            try
            {
                if (SrvSocket == null)
                {
                    SrvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                if (SrvSocket.Connected)
                {
                    if (SrvSocket.RemoteEndPoint.ToString() != strIP + ":" + iPort.ToString())
                    {
                        SrvSocket.Disconnect(true);
                    }
                }
                if (!SrvSocket.Connected)
                {
                    //从配置文件获取ip地址和端口号
                    IPEndPoint _ipep = BuildIPEndPoint(strIP, iPort);
                    if (_ipep != null)
                    {
                        AsyncCallback callback = new AsyncCallback(ConnectCallback);
                        SrvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        SrvSocket.BeginConnect(_ipep, callback, SrvSocket);
                    }
                }
            }
            catch
            {
                return;
            }

        }
        /// <summary>
        /// 心跳包
        /// </summary>
        /// <param name="onOff">是否开启</param>
        /// <param name="keepAliveTime">多久之后开始检测(ms)</param>
        /// <param name="keepAliveInterval">检测间隔(ms)</param>
        /// <returns></returns>
        private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        {
            byte[] buffer = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(buffer, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);
            return buffer;
        }


        //连接回调
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                SrvSocket.EndConnect(ar);    //结束挂起
                //myConnEvent.Set();            //设置有效信号表示已经收到数据
                OnClientConnected(new AsyncTCPClientEventArgs("已经成功连接到服务器", SrvSocket), "server cnonnect");
                //System.Diagnostics.Debug.WriteLine(String.Format("已经成功连接到服务器{0}！", _srvSocket.RemoteEndPoint.ToString()));
                //System.Diagnostics.Debug.WriteLine(String.Format("本地端接点为{0}！", _srvSocket.LocalEndPoint.ToString()));
                Rcvbuffer = new byte[MaxBufLen];     //创建一个接收缓冲区,用于存储所返回的数据

                ///每秒检测一次Socket是否正常连接
                if (IsAutoKeepAlive)
                {
                    SrvSocket.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 60000, 1000), null);
                }

                AsyncCallback callback = new AsyncCallback(ReceiveCallback);
                SrvSocket.BeginReceive(Rcvbuffer, 0, Rcvbuffer.Length, SocketFlags.None, callback, SrvSocket);

            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int iDataLen = SrvSocket.EndReceive(ar);             //这是接收回来的数据长度
                if (iDataLen > 0)
                {
                    _bReceiveData = new byte[iDataLen];               //存放已经接收返回的数据
                    Array.Copy(Rcvbuffer, 0, _bReceiveData, 0, _bReceiveData.Length);
                    //重新创建一个接收缓冲区
                    //DataProcess(_bReceiveData);
                    //ReceiveData = _bReceiveData;
                    //ReceiveEvent.Set();
                    OnDataReceived(new AsyncTCPClientEventArgs("data receive", SrvSocket), _bReceiveData);
                    //AsyncCallback callback = new AsyncCallback(ReceiveCallback);
                    //_srvSocket.BeginReceive(Rcvbuffer, 0, Rcvbuffer.Length, SocketFlags.None, callback, _srvSocket);
                }
                else
                {
                    ShutdownAndDisposeSocket();
                    OnSocketException(new AsyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "server err");
                }
            }
            catch (SocketException ex)
            {
                ShutdownAndDisposeSocket();
                OnSocketException(new AsyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "server err");
            }
            catch (Exception ex)
            {
                OnOtherException(new AsyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "other err");
            }
            finally
            {
                if (SrvSocket != null && SrvSocket.Connected == true)
                {
                    Rcvbuffer = new byte[MaxBufLen];
                    AsyncCallback callback = new AsyncCallback(ReceiveCallback);
                    SrvSocket.BeginReceive(Rcvbuffer, 0, Rcvbuffer.Length, SocketFlags.None, callback, SrvSocket);
                }
            }
        }

        /// <summary>
        /// 释放SOCKET资源与关闭
        /// </summary>
        private void ShutdownAndDisposeSocket()
        {
            if (SrvSocket != null)
            {
                SrvSocket.Shutdown(SocketShutdown.Both);
                SrvSocket.Close();
                SrvSocket = null;
            }
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="text"></param>
        public void DataProcess(byte[] Rcvbuffer)
        {
            string strText = ConvertToStrCMD(Rcvbuffer);
            _bReceiveData = null;
        }

        //=========================================================
        /// <summary>
        /// 继续回调监听数据
        /// </summary>
        protected void ContinueReceiveCallback()
        {
            //重新创建一个接收缓冲区
            Rcvbuffer = new byte[SrvSocket.SendBufferSize];
            AsyncCallback callback = new AsyncCallback(ReceiveCallback);
            SrvSocket.BeginReceive(Rcvbuffer, 0, Rcvbuffer.Length, SocketFlags.None, callback, SrvSocket);
        }

        #endregion

        #region[断开连接]

        public void DisconnectServer()
        {
            if (SrvSocket == null)
            {
                return;
            }

            if (!SrvSocket.Connected)
            {
                return;
            }

            ShutdownAndDisposeSocket();
            //System.Diagnostics.Debug.WriteLine("客户端断开了服务器的TCP连接！");

        }

        #endregion


        #region[发送数据]

        /// <summary>
        /// 发送16进制数据
        /// </summary>
        /// <param name="_bSendData"></param>
        public void SendData(byte[] _bSendData)
        {
            if (SrvSocket == null)
            {
                OnSocketException(new AsyncTCPClientEventArgs("服务器的SOCKET为NULL！", SrvSocket), "server err");
                //System.Diagnostics.Debug.WriteLine("服务器的SOCKET为NULL！");
                return;
            }

            if (!SrvSocket.Connected)
            {
                OnSocketException(new AsyncTCPClientEventArgs("尚未建立到服务器的连接", SrvSocket), "server err");
                //System.Diagnostics.Debug.WriteLine("尚未建立到服务器的连接！");
                return;
            }

            try
            {
                AsyncCallback callback = new AsyncCallback(SendCallback);
                //将数据异步发送到连接的 System.Net.Sockets.Socket
                IAsyncResult IAsync = SrvSocket.BeginSend(_bSendData, 0, _bSendData.Length, SocketFlags.None, callback, SrvSocket);
                OnDataSend(new AsyncTCPClientEventArgs("data send", SrvSocket), _bSendData);
                //System.Diagnostics.Debug.WriteLine("发送：");
            }
            catch (SocketException)
            {
                OnSocketException(new AsyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "server err");
            }
            catch
            {
                ShutdownAndDisposeSocket();
                //System.Diagnostics.Debug.WriteLine("服务器断开了TCP连接！");
                OnOtherException(new AsyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "other err");
            }
        }

        public void SendData(string strSendData)
        {

            if (strSendData == null)
            {
                return;
            }
            byte[] array = Encoding.UTF8.GetBytes(strSendData);
            SendData(array);
           
        }

        //发送数据回调
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                int sendNum = SrvSocket.EndSend(ar);    //结束挂起的异步发送
                //System.Diagnostics.Debug.WriteLine("向Socket发送字节数：" + sendNum.ToString());
            }
            catch
            {
                ShutdownAndDisposeSocket();
                //System.Diagnostics.Debug.WriteLine("服务器断开了TCP连接！");
            }
        }

        #endregion

        #region [判断是否连接]
        /// <summary>
        /// 判断是否连接
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            if (SrvSocket == null)
            {
                socketIsConnected = false;
                return false;
            }

            socketIsConnected = SrvSocket.Connected;
            return socketIsConnected;
        }
        #endregion

        protected void OnSocketException(AsyncTCPClientEventArgs e, object data)
        {
            EventHandler<AsyncTCPClientEventArgs> handler = SocketException;

            if (handler != null)
                handler(data, e);
        }

        protected void OnOtherException(AsyncTCPClientEventArgs e, object data)
        {
            EventHandler<AsyncTCPClientEventArgs> handler = OtherException;

            if (handler != null)
                handler(data, e);
        }

        protected void OnClientConnected(AsyncTCPClientEventArgs e, object data)
        {
            EventHandler<AsyncTCPClientEventArgs> handler = ClientConnected;

            if (handler != null)
                handler(data, e);
        }

        protected void OnDataReceived(AsyncTCPClientEventArgs e, object data)
        {
            EventHandler<AsyncTCPClientEventArgs> handler = DataReceived;

            if (handler != null)
                handler(data, e);
        }
        protected void OnDataSend(AsyncTCPClientEventArgs e, object data)
        {
            EventHandler<AsyncTCPClientEventArgs> handler = DataSend;

            if (handler != null)
                handler(data, e);
        }
    }
}
