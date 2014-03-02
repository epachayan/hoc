//-----------------------------------------------------------------------
// <copyright file="Node.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>07.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace HoC.Common
{
    //represents a node/server which hosts HoC service.
    [Serializable()]
    public class Node
    {
        public string Name
        {
            get;
            set;
        }

        //service endpoint for the cache service server
        public IPAddress EndPoint
        {
            get;
            set;
        }

        public DateTime HeartBeatLastHeardAt
        {
            get;
            set;
        }

        //the port on which the cache service/contract resides (not used for broadcast)
        public int ServicePort
        {
            get;
            set;
        }

        public NodeState NodeState
        {
            get;
            set;
        }

    }
}