/* Heap
 * 
 * Implements a binary heap data structure (a.k.a. priority queue).
 * 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    public class Heap<TValue>
    {
        List<KeyValuePair<double, TValue>> List = new List<KeyValuePair<double, TValue>>();
        int Size = 0;

        /// <summary>
        /// Returns the smallest key in the heap.
        /// </summary>
        public double GetMinKey()
        {
            if (Size <= 0)
                return Double.MaxValue;
            else
                return List[0].Key;
        }

        /// <summary>
        /// Returns the value that corresponds to the smallest key.
        /// </summary>
        public TValue GetMinValue()
        {
            if (Size <= 0)
                return default(TValue);
            else
                return List[0].Value;
        }

        /// <summary>
        /// Adds a new heap entry with the given key and value.
        /// The entry is added to the end of the list and then sifted up.
        /// </summary>
        public void Add(double key, TValue value)
        {
            KeyValuePair<double, TValue> kvp = new KeyValuePair<double, TValue>(key, value);
            if (Size < List.Count)
                List[Size] = kvp;
            else
                List.Add(kvp);
            int i = Size++;
            while (i > 0)
            {
                int j = (i - 1) / 2;
                if (List[j].Key <= List[i].Key)
                    break;
                KeyValuePair<double, TValue> t = List[j];
                List[j] = List[i];
                List[i] = t;
                i = j;
            }
        }

        /// <summary>
        /// Removes the entry with the smallest key value and returns it.
        /// The last entry is moved to the front and then sifted down.
        /// </summary>
        public TValue DeleteMin()
        {
            if (Size <= 0)
                return default(TValue);
            TValue result = List[0].Value;
            List[0] = List[--Size];
            int i = 0;
            while (true)
            {
                int j = 2 * i + 1;
                if (j >= Size)
                    break;
                if (j < Size - 1 && List[j + 1].Key < List[j].Key)
                    j++;
                if (List[i].Key <= List[j].Key)
                    break;
                KeyValuePair<double, TValue> t = List[j];
                List[j] = List[i];
                List[i] = t;
                i = j;
            }
            return result;
        }

        public int GetSize()
        {
            return Size;
        }
        public double getKey(int index)
        {
            return List[index].Key;
        }
        public TValue getValue(int index)
        {
            return List[index].Value;
        }
    }
}
