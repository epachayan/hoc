//-----------------------------------------------------------------------
// <copyright file="IEvictionStrategy.cs">
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
using System.Collections.Concurrent;
using HoC.Common;

namespace HoC.Server.Eviction
{
    interface IEvictionStrategy
    {
        void Evict(IEnumerable<KeyValuePair<string, ServerCacheItem>> cache);
        event EvictHandler OnEvict;
    }
}