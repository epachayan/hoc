//-----------------------------------------------------------------------
// <copyright file="HeartBeat.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>11.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using HoC.Common;

namespace HoC.Server
{
    //basically sends a signal to the client machines saying this server is alive. uses udp
    public class HeartBeat : IDisposable 
    {
        private int _heartBeatPeriod;
        private int _broadCastPort;
        private int _servicePort;
        private UdpClient _udpClient;
        private byte[] _heartBeatDataAsBytes;
        private Timer _periodic;
        private NodeState _nodeState;

        public void SendBeat()
        {
            _udpClient.Send(_heartBeatDataAsBytes, _heartBeatDataAsBytes.Length, new IPEndPoint(IPAddress.Broadcast, _broadCastPort));
        }

        public void StartSendingBeat()
        {
            _periodic.Change(_heartBeatPeriod, _heartBeatPeriod);
        }

        public void StopSendingBeat()
        {
            _periodic.Change(0, Timeout.Infinite);
        }

        private void PeriodicCallback(object state)
        {
            SendBeat();
            Trace.WriteLine("SendBeat " + NodeState.ToString());
        }

        private byte[] GetSerializedHeartBeatData()
        {
            HeartBeatData heartBeatData = new HeartBeatData() 
            { 
                IPAddress = TcpHelper.SelfIPAddress.GetAddressBytes(), 
                ServicePort = _servicePort,
                NodeState = this.NodeState //whatever beat kind is currently mentioned
            };

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream memStream = new MemoryStream();
            formatter.Serialize(memStream, heartBeatData);
            return memStream.ToArray();
        }

        internal HeartBeat(NodeState nodeState)
        {
            NodeState = nodeState;

            _heartBeatPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["HeartBeatPeriod"] ?? "3000");
            _broadCastPort = Convert.ToInt32(ConfigurationManager.AppSettings["BroadCastPort"] ?? "3001");
            _servicePort = Convert.ToInt32(ConfigurationManager.AppSettings["ServicePort"] ?? "3002"); 

            _udpClient = new UdpClient();
            _udpClient.EnableBroadcast = true;

            _periodic = new Timer(PeriodicCallback);
            StartSendingBeat();
        }

        public NodeState NodeState
        {
            get
            {
                return _nodeState;
            }

            set
            {
                _nodeState = value;
                _heartBeatDataAsBytes = GetSerializedHeartBeatData(); //reset on change
            }
        }

        public void Dispose()
        {
            _udpClient.Close();
        }
    }
}