//-----------------------------------------------------------------------
// <copyright file="ServerCacheItem.cs">
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
using System.Configuration;
using System.IO.Compression;
using System.Xml.Serialization;

namespace HoC.Common
{
    [Serializable()]
    public class ServerCacheItem
    {
        private ClientCacheItem _value;
        
        public ServerCacheItem()
        {
            LastAccessedTime = DateTime.Now;
            AccessCount = 0;
        }

        public ClientCacheItem Value
        {
            get
            {
                LastAccessedTime = DateTime.Now;
                AccessCount++;
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public DateTime LastAccessedTime
        {
            get;
            private set;
        }

        public int AccessCount
        {
            get;
            private set;
        }

        public string Hash
        {
            get;
            set;
        }

        public CacheItemState ItemState
        {
            get;
            set;
        }

        public Node RelocatedTo
        {
            get;
            set;
        }

        
    }

    [Serializable()]
    public enum CacheItemState {None=0, Moved};
}