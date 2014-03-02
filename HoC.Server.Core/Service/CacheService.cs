//-----------------------------------------------------------------------
// <copyright file="CacheService.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>12.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using HoC.Common;
using HoC.Server.Eviction;

namespace HoC.Server
{
    //we need a single server instance, running multiple threads as defined below
    //..as cache is the statefull.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class CacheService : ICacheService
    {
        Timer heartBeatResetter;
        private HeartBeat _heartBeat = new HeartBeat(NodeState.WaitingForNeighbourNode);
        private Evictor _evictor;
        private NodeState _nodeState;
        private CacheItemRelocator _cacheItemRelocator;
        private PerformanceCounter _workingSetCounter;

        private NodeState NodeState
        {
            get
            {
                return _nodeState;
            }
            set
            {
                _nodeState = value;
                _heartBeat.NodeState = _nodeState;
            }
        }

        private ConcurrentDictionary<string, ServerCacheItem> _localCache;

        public CacheService()
        {

            if (Environment.OSVersion.Version.Major >= 6) //windows 7 or above?
                _workingSetCounter = new PerformanceCounter("Process", "Working Set - Private", Process.GetCurrentProcess().ProcessName);
            else
                _workingSetCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);


            _localCache = new ConcurrentDictionary<string, ServerCacheItem>(Environment.ProcessorCount * 2, 1000);
            _evictor = new Evictor(_localCache);
            NodeState = NodeState.WaitingForNeighbourNode;

            //need to check if we recieve any information from neighbours in next 6 seconds.
            //such that they can move their data to me. note that default state is 'WaitingForOtherNode'
            heartBeatResetter = new Timer(TimerCallbackHeartBeatReset, null, 7000, Timeout.Infinite);

            _cacheItemRelocator = new CacheItemRelocator(_localCache);

            Trace.WriteLine("Initialized cacheService");
        }

        public ClientCacheItem RetrieveCacheItem(string key)
        {
            ServerCacheItem cacheItem;
            if (_localCache.TryGetValue(key, out cacheItem))
            {
                return cacheItem.Value;
            }
            else
                return null;
        }

        public void StoreCacheItem(string key, ClientCacheItem value)
        {
            ServerCacheItem cacheItemWrapped;
            if (!_localCache.TryGetValue(key, out cacheItemWrapped))
                cacheItemWrapped = new ServerCacheItem() { ItemState = CacheItemState.None };

            cacheItemWrapped.Hash = Hasher.GetHash(key);
            cacheItemWrapped.Value = value;

            Func<string, ServerCacheItem, ServerCacheItem> updateValueFactory = ((x, y) => (cacheItemWrapped));
            _localCache.AddOrUpdate(key, cacheItemWrapped, updateValueFactory);

            //has the item been moved to another node?
            if (cacheItemWrapped.ItemState == CacheItemState.Moved)
            {
                Debug.Assert(cacheItemWrapped.RelocatedTo != null);
                Node self = new Node() { EndPoint = TcpHelper.SelfIPAddress, NodeState = NodeState.Active };

                //prepare the connection
                string endPoint = string.Format("net.tcp://{0}:{1}/HoCCacheService", cacheItemWrapped.RelocatedTo.EndPoint.ToString(), cacheItemWrapped.RelocatedTo.ServicePort);
                CacheServiceReference.CacheServiceClient nodeService = new CacheServiceReference.CacheServiceClient(new NetTcpBinding(SecurityMode.None), new EndpointAddress(endPoint));

                //update to the target node.
                nodeService.StoreCacheItem(key, value);
            }

        }

        private void TimerCallbackHeartBeatReset(object state)
        {
            Trace.WriteLine("TimerCallbackHeartBeatReset called");
            NodeState = NodeState.Active;
        }

        public bool DoInterNodeObjectTransfer(Node sourceNode, string key, ServerCacheItem cacheItem)
        {
            //note that DoInterNodeObjectTransfer could be called during a shutdown of the sourceNode too
            //the below case happens only when this current node comes online
            if (NodeState == Common.NodeState.WaitingForNeighbourNode)
            {
                NodeState = NodeState.ReceivingFromOtherNode;
                heartBeatResetter.Change(6000, Timeout.Infinite); //keep resetting the timer
            }

            Func<string, ServerCacheItem, ServerCacheItem> updateValueFactory = ((x, y) => (cacheItem));
            _localCache.AddOrUpdate(key, cacheItem, updateValueFactory);
            return true;
        }

        public bool EndInterNodeObjectTransfer(Node sourceNode)
        {
            NodeState = NodeState.Active; //get online
            _heartBeat.SendBeat();//force a beat now
            
            heartBeatResetter.Change(0, Timeout.Infinite);//dont want the timer to fire henceforth
            return true;
        }


        public bool Stop()
        {
            _cacheItemRelocator.PerformShutdownMove();
            _heartBeat.StopSendingBeat();
            return true;
        }


        public CacheHealth GetCacheHealth()
        {
            long objectSize = (from cacheItem in _localCache select cacheItem.Value.Value.Value.LongLength).Sum();
             
            CacheHealth cacheHealth = new CacheHealth() { 
                ObjectCount = _localCache.Count,
                TotalObjectSize = objectSize,
                EvictionStrategy = _evictor.EvictionClass,
                ProcessWorkingSet = _workingSetCounter.RawValue/1024,
                EvictionLastAt = _evictor.LastRunTime,
                MachineName = Environment.MachineName
            };

            return cacheHealth;
        }
    }

}