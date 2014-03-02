//-----------------------------------------------------------------------
// <copyright file="Form1.cs">
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HoC.Client;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HoC.Test.Harness
{
    public partial class QuickTest : Form
    {
        private Cache _cache = new Cache();
        Random random = new Random();
        Dictionary<string, string> dict = new Dictionary<string, string>();

        public QuickTest()
        {
            InitializeComponent();
        }


        private void button5_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            _cache["object1"] = "Hello there";
            _cache["object1"] = "Yo yo";
            _cache["object2"] = "Nice";
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Object objectFromCache = _cache["object1"];
            if (objectFromCache != null)
                MessageBox.Show(objectFromCache.ToString());
            else
                MessageBox.Show("object1 is empty");

            objectFromCache = _cache["object2"];
            if (objectFromCache != null)
                MessageBox.Show(objectFromCache.ToString());
            else
                MessageBox.Show("object2 is empty");

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            for (int value = 1; value < 1000; value++)
            {
                string randomValue = random.Next().ToString() + "_" + value.ToString();
                _cache[value.ToString()] = randomValue;
            }

            watch.Stop();
            MessageBox.Show("Completed write in (ms)" + watch.ElapsedMilliseconds.ToString());

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            //simple get
            for (int value = 1; value < 1000; value++)
            {
                string randomValue = _cache[value.ToString()].ToString();
            }

            watch.Stop();
            MessageBox.Show("Completed read in (ms)" + watch.ElapsedMilliseconds.ToString());
        }
    }
}