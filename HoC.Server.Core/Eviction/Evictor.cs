//-----------------------------------------------------------------------
// <copyright file="Evictor.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>02.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Configuration;
using HoC.Common;

namespace HoC.Server.Eviction
{
    //calls the eviction strategy that is active in config
    class Evictor
    {
        Timer _evictionTimer;
        int _evictionPeriod;
        IEvictionStrategy _evictionStrategy;
        ConcurrentDictionary<string, ServerCacheItem> _cache;
        string _evictionClass;
        DateTime _evictionLastRunAt = DateTime.MinValue;

        public void Evict()
        {
            _evictionLastRunAt = DateTime.Now;
            EvictionStrategy.Evict(_cache);
        }

        public Evictor(ConcurrentDictionary<string, ServerCacheItem> dictionary)
        {
            _cache = dictionary;
            _evictionPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["EvictionPeriod"] ?? "60000");
            _evictionTimer = new Timer(AtEvictionPeriod, null, _evictionPeriod, _evictionPeriod);
            _evictionClass = ConfigurationManager.AppSettings["EvictionStrategyClass"] ?? "HoC.Server.Eviction.LRUEvictionStrategy";
        }

        private void AtEvictionPeriod(object state)
        {
            Evict();
        }

        private IEvictionStrategy EvictionStrategy
        {
           get
            {
                if (_evictionStrategy == null)
                {
                    Type evictionType = Type.GetType(_evictionClass);
                    _evictionStrategy = ((IEvictionStrategy)Activator.CreateInstance(evictionType, null));
                    _evictionStrategy.OnEvict += new EvictHandler(OnEviction);
                }

                return _evictionStrategy;
            }
        }

        //the eviction strategy lets know when the eviction is effective. 
        //upto the cache to remove the item
        private void OnEviction(KeyValuePair<string, ServerCacheItem> valuePair)
        {
            ServerCacheItem cacheItem;
            _cache.TryRemove(valuePair.Key, out cacheItem);
        }

        public string EvictionClass
        {
            get
            {
                return _evictionClass;
            }
        }

        public DateTime LastRunTime
        {
            get
            {
                return _evictionLastRunAt;
            }
        }
    }

    public delegate void EvictHandler(KeyValuePair<string, ServerCacheItem> valuePair);
}