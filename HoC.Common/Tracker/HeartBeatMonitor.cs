//-----------------------------------------------------------------------
// <copyright file="HeartBeatMonitor.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>24.Aug.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Configuration;
using HoC.Common;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;


namespace HoC.Common
{
    //would listen for message from HoC servers
    internal class HeartBeatMonitor : IDisposable
    {
        private int _protocolPort;
        Socket _udpClient;
        byte[] recBuffer = new byte[1024];

        public delegate void HeartBeatReceivedEventHandler(HeartBeatData heartBeatData);
        public event HeartBeatReceivedEventHandler OnHeartBeatReceived;

        internal HeartBeatMonitor()
        {
            _protocolPort = Convert.ToInt32(ConfigurationManager.AppSettings["BroadCastPort"] ?? "3001");
            _udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1); 
            _udpClient.Bind(new IPEndPoint(IPAddress.Any, _protocolPort)); 

            Listen();
        }

        void Listen()
        {
            _udpClient.BeginReceive(recBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(MessageReceivedCallback), null);
        }

        void MessageReceivedCallback(IAsyncResult result)
        {
            if (OnHeartBeatReceived != null)
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, _protocolPort);
                SocketError socketErrorCode;
                _udpClient.EndReceive(result, out socketErrorCode);
                HeartBeatData heartBeatData = GetHeartBeatData(recBuffer);

                OnHeartBeatReceived(heartBeatData);
            }

            Listen();
        }

        private HeartBeatData GetHeartBeatData(byte[] heartBeatDataAsBytes)
        {
            if (heartBeatDataAsBytes.Length == 0)
                throw new HeartBeatDataIsEmptyException();

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream memStream = new MemoryStream(heartBeatDataAsBytes);
            HeartBeatData heartBeatdata = (HeartBeatData)formatter.Deserialize(memStream);
            return heartBeatdata;
        }



        public void Dispose()
        {
            _udpClient.Close();
        }
    }

    public class HeartBeatDataIsEmptyException : ApplicationException
    {
    }

}