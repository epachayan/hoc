//-----------------------------------------------------------------------
// <copyright file="CacheItemRelocator.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>16.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using HoC.Common;


namespace HoC.Server
{
    /// <summary>
    /// moves the cache item to other servers in scenarios of new node arrival / node shutdown
    /// assumes that the load during this time is minimal.
    /// assumes that each operation is done in atomic manner - one node brought online/shutdown at a time
    /// </summary>
    class CacheItemRelocator
    {
        private ConcurrentDictionary<string, ServerCacheItem> _cache;
        protected NodeTracker _nodeTracker = NodeTrackerSingleton.Instance;
        private HashNodeResolver _nodeResolver = new HashNodeResolver();
        private string _selfKeyHash = Hasher.GetHash(TcpHelper.SelfIPAddress.ToString());

        private bool NodeIsNeighbourBehind(Node node)
        {
            //add the new node to a temporary clone of the active consistent hash
            ConsistentHash consistentHashTrialRun = _nodeResolver.GetActiveConsistentHashClone();
            consistentHashTrialRun.StoreItem(node.EndPoint.ToString());

            //ask the consistent hash that if the node was around, would that node be my immediate previous neighbour?
            string previousItem = consistentHashTrialRun.GetPreviousItemInCircle(TcpHelper.SelfIPAddress.ToString());
            if (node.EndPoint.ToString().CompareTo(previousItem) == 0)
                return true;

            return false;
        }

        public CacheItemRelocator(ConcurrentDictionary<string, ServerCacheItem> dictionary)
        {
            _cache = dictionary;
            _nodeTracker.OnNodeListUpdated += new NodeTracker.NodeListUpdatedHandler(NodeListUpdated);
        }

        private void NodeListUpdated(Node node, NodeUpdateAction updateAction)
        {
            //has the node just come online and waiting for its data that I have?
            if ((updateAction == NodeUpdateAction.Added) && (node.NodeState == NodeState.WaitingForNeighbourNode))
            {
                //was that node me?
                if (node.EndPoint.ToString().CompareTo(TcpHelper.SelfIPAddress.ToString()) == 0)
                    return;

                bool nodeIsNeighbourBehind = NodeIsNeighbourBehind(node);
                    
                if (!nodeIsNeighbourBehind)
                    return;

                ThreadPool.QueueUserWorkItem(MoveObjectCallBack, node);
            }
        }


        public void MoveObjectCallBack(object state)
        {
            Node targetNode = (Node)state;
            string nodeKeyHash = Hasher.GetHash(targetNode.EndPoint.ToString());
            string previousNodeHash = Hasher.GetHash(_nodeResolver.GetPreviousItemInCircle(targetNode.EndPoint.ToString()));

            //if self is B, typical scenarios -> A-newnode-MID-B, MID-A-newnode-B-C
            //A being the previous node, B being the current node and newnode being the new node.
            string lowerBound = previousNodeHash;
            string upperBound = nodeKeyHash;
            IEnumerable<KeyValuePair<string, ServerCacheItem>> candidateList = _cache.Where<KeyValuePair<string, ServerCacheItem>>(x =>
                ((x.Value.Hash.CompareTo(upperBound) < 0) &&
                (x.Value.Hash.CompareTo(lowerBound) > 0)));

            //special case: A-MID-newnode-B
            if (lowerBound.CompareTo(upperBound) > 0)
            {
                candidateList = _cache.Where<KeyValuePair<string, ServerCacheItem>>(x =>
                ((x.Value.Hash.CompareTo(upperBound) < 0) ||
                (x.Value.Hash.CompareTo(lowerBound) > 0)));
            }

            if (candidateList.Count() == 0)
                return;

            Node self = new Node() { EndPoint = TcpHelper.SelfIPAddress, NodeState= NodeState.Active};

            //prepare the connection
            string endPoint = string.Format("net.tcp://{0}:{1}/HoCCacheService", targetNode.EndPoint.ToString(), targetNode.ServicePort);
            CacheServiceReference.CacheServiceClient nodeService = new CacheServiceReference.CacheServiceClient(new NetTcpBinding(SecurityMode.None), new EndpointAddress(endPoint));
            try
            {
                //move each of the object to the other node async
                foreach (KeyValuePair<string, ServerCacheItem> valuePair in candidateList)
                {
                    nodeService.DoInterNodeObjectTransfer(self, valuePair.Key, valuePair.Value);
                    valuePair.Value.ItemState = CacheItemState.Moved;
                    valuePair.Value.RelocatedTo = targetNode;
                };
                nodeService.EndInterNodeObjectTransfer(self);

                Thread.Sleep(2000);//wait the current thread - note that this is not the main thread
                RemoveOrResetMovedItems(targetNode);

                //note that any object update that happens during this time is handled by the CacheService.
                //basically it copies the update to the new node too.
            }
            catch
            {
                ResetMovedObjectState(); //in case of any issues revert back to normal state.
            }
        }


        private void RemoveOrResetMovedItems(Node targetNode)
        {
            Func<Node, bool> nodeCompare = x => (x.EndPoint.ToString().CompareTo(targetNode.EndPoint.ToString()) == 0);
            Node targetNodeInternal = _nodeTracker.ActiveNodes.First<Node>(nodeCompare);

            if (targetNodeInternal == null)
                ResetMovedObjectState();
            else if (targetNodeInternal.NodeState == NodeState.Active) //is the target node now active?
                RemoveMovedObjectState();
            else
                ResetMovedObjectState(); // reset all moved states as the node does not appear to have got online yet
        }

        private void ResetMovedObjectState()
        {
            IEnumerable<KeyValuePair<string, ServerCacheItem>> candidateList = _cache.Where<KeyValuePair<string, ServerCacheItem>>(x => x.Value.ItemState == CacheItemState.Moved);
            Parallel.ForEach<KeyValuePair<string, ServerCacheItem>>(candidateList, valuePair =>
            {
                valuePair.Value.ItemState = CacheItemState.None;
                valuePair.Value.RelocatedTo = null;
            });
        }

        private void RemoveMovedObjectState()
        {
            IEnumerable<KeyValuePair<string, ServerCacheItem>> candidateList = _cache.Where<KeyValuePair<string, ServerCacheItem>>(x => x.Value.ItemState == CacheItemState.Moved);
            Parallel.ForEach<KeyValuePair<string, ServerCacheItem>>(candidateList, valuePair =>
            {
                ServerCacheItem removeItem;
                _cache.TryRemove(valuePair.Key, out removeItem);
            });
        }

        public void PerformShutdownMove()
        {
            //targetnode is the one which comes next in the circle, effectively returned by GetObjectLocation
            Node targetNode = _nodeResolver.GetObjectLocation(TcpHelper.SelfIPAddress.ToString());

            //is targetnode self? (am I the only node in the circle?)
            if (targetNode.EndPoint.ToString().CompareTo(TcpHelper.SelfIPAddress.ToString()) == 0)
                return;

            string endPoint = string.Format("net.tcp://{0}:{1}/HoCCacheService", targetNode.EndPoint.ToString(), targetNode.ServicePort);
            CacheServiceReference.CacheServiceClient nodeService = new CacheServiceReference.CacheServiceClient(new NetTcpBinding(SecurityMode.None), new EndpointAddress(endPoint));

            Node self = new Node() { EndPoint = TcpHelper.SelfIPAddress, NodeState= NodeState.Active};

            //move the objects to the nearest node async
            Parallel.ForEach<KeyValuePair<string, ServerCacheItem>>(_cache, valuePair =>
            {
                nodeService.DoInterNodeObjectTransfer(self, valuePair.Key, valuePair.Value);
                valuePair.Value.ItemState = CacheItemState.Moved;
                valuePair.Value.RelocatedTo = targetNode; 
            });
        }
    }
}