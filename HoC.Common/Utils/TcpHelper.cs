//-----------------------------------------------------------------------
// <copyright file="TcpHelper.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>11.Aug.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;


namespace HoC.Common
{
    public static class TcpHelper
    {
        public static IPAddress SelfIPAddress
        {
            get
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
                IPAddress internetIPAddress = addresses.First<IPAddress>(x => x.AddressFamily == AddressFamily.InterNetwork);
                return internetIPAddress;
            }
        }
    }
}