//-----------------------------------------------------------------------
// <copyright file="NodeTracker.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>12.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HoC.Common;
using System.Net;
using System.Configuration;
using System.Threading;

namespace HoC.Common
{
    //uses the help of HeartBeatListener to track the active nodes
    //also maintains a local time to invalidate dead nodes
    public class NodeTracker
    {
        private List<Node> _activeNodes = new List<Node>();
        private HeartBeatMonitor _heartBeatMonitor = new HeartBeatMonitor();
        private Timer _listInvalidator ;
        private const int NodeInvalidatePeriod = 12000;
        private int _waitTimeTillNodeDeath = 3000 * 3;

        public delegate void NodeListUpdatedHandler(Node node, NodeUpdateAction updateAction);
        public event NodeListUpdatedHandler OnNodeListUpdated;
       
        public IEnumerable<Node> ActiveNodes
        {
            get
            {
                return _activeNodes;
            }
        }

        public NodeTracker()
        {
            _waitTimeTillNodeDeath = Convert.ToInt32(ConfigurationManager.AppSettings["WaitTimeTillNodeDeath"] ?? "4000");
            _heartBeatMonitor.OnHeartBeatReceived += new HeartBeatMonitor.HeartBeatReceivedEventHandler(HeartBeatTracker);
            _listInvalidator = new Timer(InvalidateCallback, null, NodeInvalidatePeriod, NodeInvalidatePeriod);
        }

        private void InvalidateCallback(object state)
        {
            RemoveDeadNodes();
        }

        private void RemoveDeadNodes()
        {
            List<Node> deadNodeList = new List<Node>();

            foreach (Node node in _activeNodes)
            {
                lock (node)
                {
                    //check to see if we have missed a couple of heartbeats.
                    //assume node is dead if we havent heard in "WaitTimeTillNodeDeath" duration.
                    if (node.HeartBeatLastHeardAt.AddMilliseconds(_waitTimeTillNodeDeath) < DateTime.Now)
                    {
                        deadNodeList.Add(node);
                        //on server, need a handler to move objects to this new node..
                    }
                }
            }

            lock (_activeNodes)
            {
                foreach (Node node in deadNodeList)
                {
                    _activeNodes.Remove(node);
                }
            }

            //sent notification to all interested parties
            if (OnNodeListUpdated != null)
                foreach (Node node in deadNodeList)
                {
                    OnNodeListUpdated(node, NodeUpdateAction.Removed);
                }

        }


        private void HeartBeatTracker(HeartBeatData heartBeatData)
        {
            IPAddress nodeIPAddress = new IPAddress(heartBeatData.IPAddress);
            Node node = _activeNodes.Find(x => x.EndPoint.ToString() == nodeIPAddress.ToString());

            NodeUpdateAction updateAction;

            if (node != null)
            {
                //update the last heard time, node state
                node.HeartBeatLastHeardAt = DateTime.Now; 
                node.NodeState = heartBeatData.NodeState;
                node.EndPoint = nodeIPAddress;
                node.ServicePort = heartBeatData.ServicePort;

                updateAction = NodeUpdateAction.Updated;
            }
            else
            {
                node = new Node()
                {
                    EndPoint = nodeIPAddress,
                    ServicePort = heartBeatData.ServicePort,
                    HeartBeatLastHeardAt = DateTime.Now,
                    NodeState = heartBeatData.NodeState
                };

                lock (_activeNodes)
                {
                    _activeNodes.Add(node); //add the new node which appears to have just come online.
                }

                updateAction = NodeUpdateAction.Added;
            }

            if (OnNodeListUpdated != null)
                OnNodeListUpdated(node, updateAction);
        }

    }

    public static class NodeTrackerSingleton
    {
        private static NodeTracker _instance;
        public static NodeTracker Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NodeTracker();

                return _instance;
            }
        }
    }

    public enum NodeUpdateAction { Added, Updated, Removed };
}