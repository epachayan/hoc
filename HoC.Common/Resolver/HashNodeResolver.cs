//-----------------------------------------------------------------------
// <copyright file="HashNodeResolver.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>28.Aug.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace HoC.Common
{
    public class HashNodeResolver
    {
        protected ConsistentHash _consistentHash = new ConsistentHash();
        protected NodeTracker _nodeTracker = NodeTrackerSingleton.Instance;

        //gets the nearest possible location where this item would be.
        public Node GetObjectLocation(string key)
        {
            try
            {
                //ask the consistent hash the possible nearest node
                string nearestNodeAddress = _consistentHash.GetNearestItem(key);
                Node nearestNode = _nodeTracker.ActiveNodes.First<Node>(x => x.EndPoint.ToString() == nearestNodeAddress);
                return nearestNode;
            }
            catch (ConsistentHashCircleEmpty)
            {
                throw new NodeListEmptyException("No nodes are currently active");
            }
        }

        public HashNodeResolver()
        {
            _nodeTracker.OnNodeListUpdated += new NodeTracker.NodeListUpdatedHandler(NodeListUpdated);
        }

        private void NodeListUpdated(Node node, NodeUpdateAction updateAction)
        {
            //not interested in any non-active nodes
            //other states usually happen when a node comes up online only
            if (!(node.NodeState == NodeState.Active))
                return;

            if (updateAction == NodeUpdateAction.Removed)
                _consistentHash.RemoveItem(node.EndPoint.ToString());
            else if ((updateAction == NodeUpdateAction.Added) || (updateAction == NodeUpdateAction.Updated))
                _consistentHash.StoreItem(node.EndPoint.ToString());
        }

        public string GetPreviousItemInCircle(string key)
        {
            try
            {
                return _consistentHash.GetPreviousItemInCircle(key);
            }
            catch (ConsistentHashCircleEmpty)
            {
                return String.Empty;
            }
        }

        public ConsistentHash GetActiveConsistentHashClone()
        {
            return (ConsistentHash)_consistentHash.Clone();
        }
    }

    public class NodeListEmptyException : ApplicationException
    {
        public NodeListEmptyException(string exceptionMessage):base(exceptionMessage){}
    }
}