//-----------------------------------------------------------------------
// <copyright file="ClientCacheItem.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>09.Aug.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace HoC.Common
{
    [DataContract()]
    [Serializable()]
    public class ClientCacheItem
    {
        [DataMember()]
        public string TypeAsString
        {
            get;
            set;
        }

        [DataMember()]
        public byte[] Value
        {
            get;
            set;
        }
    }
}