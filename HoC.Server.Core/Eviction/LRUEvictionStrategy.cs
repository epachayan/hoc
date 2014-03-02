//-----------------------------------------------------------------------
// <copyright file="LRUEvictionStrategy.cs">
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
    //implements the LRU eviction stategy, evicts only one element in each eviction
    //in this implementation.
    public class LRUEvictionStrategy : IEvictionStrategy
    {
        private int _evictionPercent;

        public void Evict(IEnumerable<KeyValuePair<string, ServerCacheItem>> cache)
        {
            if (OnEvict == null)
                return;
            
            int numberOfItemsToBeEvicted = (cache.Count() * _evictionPercent /100);
             
            if (numberOfItemsToBeEvicted < 1)
                return;
            
            var result = 
                (from cacheItem in cache
                orderby cacheItem.Value.LastAccessedTime
                select cacheItem).Take(numberOfItemsToBeEvicted);

            foreach(KeyValuePair<string, ServerCacheItem> cacheItem in result)
                OnEvict(cacheItem);
        }

        public event EvictHandler OnEvict;

        public LRUEvictionStrategy()
        {
            _evictionPercent = Convert.ToInt32(ConfigurationManager.AppSettings["EvictionPercent"] ?? "5"); //defaults to 5%
        }
    }
}