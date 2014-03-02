//-----------------------------------------------------------------------
// <copyright file="CacheBasicTest.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>07.Aug.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using HoC.Client;

namespace HoC.Test
{
    [TestClass]
    public class CacheBasicTest
    {
        private Cache _cache;

        [TestInitialize()]
        public void Initialize()
        {
            _cache = new Cache();
            Thread.Sleep(5000); //need to wait a couple of moments before the nodes are identified
        }

        [TestCleanup()]
        public void CleanUp()
        {
            //
        }

        [TestMethod]
        public void SaveRetrieveString()
        {
            _cache["object1"] = "Hel    lo ther";
            _cache["object1"] = "Yo yo";
            _cache["object2"] = "Nice";

            Assert.IsTrue(_cache["object1"].ToString().CompareTo("Yo yo") == 0);
            Assert.IsTrue(_cache["object2"].ToString().CompareTo("Nice") == 0);
        }

        [TestMethod]
        public void StudentObjectTest()
        {
            _cache["Tomy"] = new Student() { Name = "Tomy", Age = 35 };
            _cache["Wilfred"] = new Student() { Name = "Wilfred", Age = 23};

            Assert.IsTrue(((Student)_cache["Tomy"]).Name == "Tomy");
            Assert.IsTrue(((Student)_cache["Tomy"]).Age == 35);

            Assert.IsTrue(((Student)_cache["Wilfred"]).Name == "Wilfred");
            Assert.IsTrue(((Student)_cache["Wilfred"]).Age == 23);
        }
    }

    [Serializable]
    public class Student
    {
        public string Name
        {
            get;
            set;
        }

        public int Age
        {
            get;
            set;
        }
    } 
}