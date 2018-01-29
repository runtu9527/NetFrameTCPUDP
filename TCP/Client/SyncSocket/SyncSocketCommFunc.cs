using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFrame.TCP.Client.SyncSocket
{
    public class SyncSocketCommFunc : SyncSocketCommProperty
    {
        protected byte[] ConvertToBytes(string strSendCMD)
        {
            byte[] aryData = Encoding.UTF8.GetBytes(strSendCMD);
            return aryData;
        }

        protected string ConvertToStrCMD(byte[] byteData)
        {
            if (byteData != null && byteData.Length > 0)
            {
                string strText = Encoding.UTF8.GetString(byteData, 0, byteData.Length);
                return strText;
            }

            return string.Empty;
        }




    }
}
