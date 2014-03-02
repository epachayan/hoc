//-----------------------------------------------------------------------
// <copyright file="MultiClientTest.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>03.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HoC.Client;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestClass]
    public class MultiClientTest
    {
        List<Cache> cacheList;
        const int _cacheCount = 10;
        const int _objectCount = 1000;

        Random random = new Random();

        [TestInitialize()]
        public void Initialize()
        {

            cacheList = new List<Cache>();
            for (int i = 0; i < _cacheCount; i++)
            {
                cacheList.Add(new Cache());
            }

            Thread.Sleep(10000); //need to wait a couple of moments before the nodes are identified
        }


        [TestMethod]
        public void TestManyCaches()
        {
            //parallelise for multiclient simulation
            Parallel.ForEach(cacheList, cache =>
                {
                    Parallel.For(0, _objectCount, integer =>
                        {
                            string randomValue = random.Next().ToString() + "_" + integer.ToString();
                            cache[integer.ToString()] = randomValue; 
                            Assert.IsNotNull(cache[integer.ToString()]); //have to do the check immediately before evictor runs
                        });
                });
        }
    }
}