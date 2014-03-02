//-----------------------------------------------------------------------
// <copyright file="CacheHealth.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>02.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HoC.Common
{
    [Serializable()]
    public class CacheHealth
    {
        public int ObjectCount { get; set; }
        public long TotalObjectSize { get; set; }
        public string EvictionStrategy { get; set; }
        public long ProcessWorkingSet { get; set; }
        public long FreeRAM { get; set; }
        public long TotalRAM { get; set; }
        public TimeSpan ProcessCPUUsage { get; set; }
        public DateTime EvictionLastAt { get; set; }
        public string MachineName { get; set; }
    }

}