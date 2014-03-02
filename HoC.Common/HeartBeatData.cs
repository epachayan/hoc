//-----------------------------------------------------------------------
// <copyright file="HeartBeatData.cs">
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
using System.Runtime.InteropServices;

namespace HoC.Common
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct HeartBeatData
    {
        public byte[] IPAddress;
        public int ServicePort;//not used for heartbeat
        public NodeState NodeState;
    }

}