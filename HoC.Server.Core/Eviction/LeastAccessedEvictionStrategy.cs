//-----------------------------------------------------------------------
// <copyright file="LeastAccessedEvictionStrategy.cs">
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
using System.Configuration;
using HoC.Common;

namespace HoC.Server.Eviction
{
    //evicts the least accessed item from the cache
    //current implementation evicts only a single item.
    public class LeastAccessedEvictionStrategy : IEvictionStrategy
    {
        private int _evictionPercent;
        public void Evict(IEnumerable<KeyValuePair<string, ServerCacheItem>> cache)
        {
            if (OnEvict == null)
                return;

            int numberOfItemsToBeEvicted = (cache.Count() * _evictionPercent / 100);

            if (numberOfItemsToBeEvicted < 1)
                return;

            var result =
                (from cacheItem in cache
                 orderby cacheItem.Value.AccessCount
                 select cacheItem).Take(numberOfItemsToBeEvicted);

            foreach (KeyValuePair<string, ServerCacheItem> cacheItem in result)
                OnEvict(cacheItem);

        }

        public LeastAccessedEvictionStrategy()
        {
            _evictionPercent = Convert.ToInt32(ConfigurationManager.AppSettings["EvictionPercent"] ?? "5"); //defaults to 5%
        }


        public event EvictHandler OnEvict;
    }
            
}