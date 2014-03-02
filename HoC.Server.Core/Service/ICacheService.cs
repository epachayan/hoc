//-----------------------------------------------------------------------
// <copyright file="ICacheService.cs">
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
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using HoC.Common;

namespace HoC.Server
{
    [ServiceContract()]
    interface ICacheService
    {
        [OperationContract]
        ClientCacheItem RetrieveCacheItem(string key);
        [OperationContract(IsOneWay=true)]
        void StoreCacheItem(string key, ClientCacheItem value);
        [OperationContract]
        bool DoInterNodeObjectTransfer(Node sourceNode, string key, ServerCacheItem cacheItem);
        [OperationContract]
        bool EndInterNodeObjectTransfer(Node sourceNode);
        [OperationContract]
        bool Stop();
        [OperationContract]
        CacheHealth GetCacheHealth();
    }
}