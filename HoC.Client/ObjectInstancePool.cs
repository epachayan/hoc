//-----------------------------------------------------------------------
// <copyright file="ObjectInstancePool.cs">
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
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace HoC.Client
{
    /// <summary>
    /// maintains a pool/list of objects, provides it on request
    /// usually used for classes whose construction call is heavy
    /// </summary>
    /// <typeparam name="W"></typeparam>
    public class ObjectInstancePool<W>
    {
        private const int objectCount = 20; //can be updated to receive through the constructor
        private ConcurrentBag<W> objectList = new ConcurrentBag<W>();

        public ObjectInstancePool()
        {
            //refresh the bag first
            new AsyncMethodCaller(Filler).BeginInvoke(null, null);
        }

        public W GetInstance()
        {
            W result;

            if (!(objectList.TryTake(out result)))
            {
                result = Activator.CreateInstance<W>();
            }
            else
                new AsyncMethodCaller(Filler).BeginInvoke(null, null); //refresh the bag
            
            return result;
        }

        public delegate void AsyncMethodCaller();

        private void Filler()
        {
            while (objectList.Count < objectCount)
            {
                objectList.Add(Activator.CreateInstance<W>());
            }
        }
    }
}