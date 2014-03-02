//-----------------------------------------------------------------------
// <copyright file="ConsistentHash.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>28.Aug.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace HoC.Common
{
    public class ConsistentHash  : ICloneable
    {
        SortedList<string, string> itemCircle = new SortedList<string, string>();

        public string GetNearestItem(string key)
        {
            if (itemCircle.Count == 0)
                throw new ConsistentHashCircleEmpty();

            string keyHash = Hasher.GetHash(key);

            //find the last item that is just after the passed in key (clockwise)
            Func<KeyValuePair<string, string>, bool> stringCompare = x => (x.Key.CompareTo(keyHash) > 0);
            KeyValuePair<string, string> item = itemCircle.FirstOrDefault(stringCompare);

            if (string.IsNullOrEmpty(item.Key)) 
            {
                //if no item, fallback to the first item => traverse circle clockwise 
                return itemCircle.First().Value; 
            }

            return item.Value;
        }

        public void StoreItem(string item)
        {
            string hash = Hasher.GetHash(item);
            itemCircle[hash] = item;
        }

        public void RemoveItem(string item)
        {
            string hash = Hasher.GetHash(item);
            if (itemCircle.ContainsKey(hash))
                itemCircle.Remove(hash);
        }

        public string GetNextItemInCircle(string key)
        {
            return GetNearestItem(key);
        }

        //this function traverses anticlockwise, whereas GetNearestItem() traverses clockwise
        public string GetPreviousItemInCircle(string key)
        {
            if (itemCircle.Count == 0)
                throw new ConsistentHashCircleEmpty();

            string keyHash = Hasher.GetHash(key);

            //traverse, anticlock wise , find first item
            Func<KeyValuePair<string, string>, bool> stringCompare = x => (x.Key.CompareTo(keyHash) < 0);
            KeyValuePair<string, string> item = itemCircle.FirstOrDefault(stringCompare);

            if (string.IsNullOrEmpty(item.Key))
            {
                //if no item, fallback to the last item => traverse circle anti clockwise 
                return itemCircle.Last().Value;
            }

            return item.Value;
        }



        public object Clone()
        {
            return (ConsistentHash)MemberwiseClone();
        }
    }

    class ConsistentHashCircleEmpty : ApplicationException
    {

    }


}