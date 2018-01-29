using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetFrame.TCP.Client.SyncSocket
{
    /// <summary>
    /// 所有的数据，都通过此类接口发送到终端服务器中
    /// 异步通信
    /// </summary>
    public class SyncSocketComm : SyncSocketCommFunc
    {
        public event EventHandler<SyncTCPClientEventArgs> DataReceived;
        public event EventHandler<SyncTCPClientEventArgs> DataSend;
        public event EventHandler<SyncTCPClientEventArgs> ClientConnected;
        public event EventHandler<SyncTCPClientEventArgs> SocketException;
        public event EventHandler<SyncTCPClientEventArgs> OtherException;

        #region [构建Socket]
        /// <summary>
        ///  构建Socket
        /// </summary>
        /// <param name="strIP"></param>
        /// <param name="iPort"></param>
        public SyncSocketComm(bool isAutoKeepAlive = false)
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

        #region[同步接收数据]

        /// <summary>
        /// 同步连接
        /// </summary>
        /// <param name="strIP"></param>
        /// <param name="iPort"></param>
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
                        //AsyncCallback callback = new AsyncCallback(ConnectCallback);
                        SrvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        //SrvSocket.BeginConnect(_ipep, callback, SrvSocket);
                        //尝试连接
                        try
                        {
                            SrvSocket.Connect(_ipep);
                        }
                        //异常处理
                        catch (SocketException e)
                        {
                            OnSocketException(new SyncTCPClientEventArgs("连接失败", SrvSocket), "connect err");
                            return;
                        }
                        if (SrvSocket.Connected)
                        {
                            OnClientConnected(new SyncTCPClientEventArgs("已经成功连接到服务器", SrvSocket), "server cnonnect");
                        }

                        SyncReceiveData();
                    }
                }
            }
            catch
            {
                OnOtherException(new SyncTCPClientEventArgs("创建连接失败", SrvSocket), "connect err");
                return;
            }

        }


        private void SyncReceiveData()
        {
            while (true)
            {
                try
                {
                    Rcvbuffer = new byte[MaxBufLen];     //创建一个接收缓冲区,用于存储所返回的数据
                   
                    //定义接收到的数据的长度
                    int recv = SrvSocket.Receive(Rcvbuffer, SocketFlags.None);
                    _bReceiveData = new byte[recv];               //存放已经接收返回的数据
                    Array.Copy(Rcvbuffer, 0, _bReceiveData, 0, _bReceiveData.Length);
                    //Console.WriteLine(_bReceiveData.Length);
                    OnDataReceived(new SyncTCPClientEventArgs("data receive", SrvSocket), _bReceiveData);
                }
                catch (SocketException ex)
                {
                    ShutdownAndDisposeSocket();
                    OnSocketException(new SyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "server err");
                    return;
                }
                catch (Exception ex)
                {
                    OnOtherException(new SyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "other err");
                    return;
                }
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
                OnSocketException(new SyncTCPClientEventArgs("服务器的SOCKET为NULL！", SrvSocket), "server err");
                //System.Diagnostics.Debug.WriteLine("服务器的SOCKET为NULL！");
                return;
            }

            if (!SrvSocket.Connected)
            {
                OnSocketException(new SyncTCPClientEventArgs("尚未建立到服务器的连接", SrvSocket), "server err");
                //System.Diagnostics.Debug.WriteLine("尚未建立到服务器的连接！");
                return;
            }

            try
            {
                AsyncCallback callback = new AsyncCallback(SendCallback);
                //将数据异步发送到连接的 System.Net.Sockets.Socket
                IAsyncResult IAsync = SrvSocket.BeginSend(_bSendData, 0, _bSendData.Length, SocketFlags.None, callback, SrvSocket);
                OnDataSend(new SyncTCPClientEventArgs("data send", SrvSocket), _bSendData);
                //System.Diagnostics.Debug.WriteLine("发送：");
            }
            catch (SocketException)
            {
                OnSocketException(new SyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "server err");
            }
            catch
            {
                ShutdownAndDisposeSocket();
                //System.Diagnostics.Debug.WriteLine("服务器断开了TCP连接！");
                OnOtherException(new SyncTCPClientEventArgs("服务器已断开连接", SrvSocket), "other err");
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

        protected void OnSocketException(SyncTCPClientEventArgs e, object data)
        {
            EventHandler<SyncTCPClientEventArgs> handler = SocketException;

            if (handler != null)
                handler(data, e);
        }

        protected void OnOtherException(SyncTCPClientEventArgs e, object data)
        {
            EventHandler<SyncTCPClientEventArgs> handler = OtherException;

            if (handler != null)
                handler(data, e);
        }

        protected void OnClientConnected(SyncTCPClientEventArgs e, object data)
        {
            EventHandler<SyncTCPClientEventArgs> handler = ClientConnected;

            if (handler != null)
                handler(data, e);
        }

        protected void OnDataReceived(SyncTCPClientEventArgs e, object data)
        {
            EventHandler<SyncTCPClientEventArgs> handler = DataReceived;

            if (handler != null)
                handler(data, e);
        }
        protected void OnDataSend(SyncTCPClientEventArgs e, object data)
        {
            EventHandler<SyncTCPClientEventArgs> handler = DataSend;

            if (handler != null)
                handler(data, e);
        }
    }
}
