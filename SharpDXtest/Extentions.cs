using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest
{
    public static class Extentions
    {
        public static void AddRange<K, V>(this IDictionary<K, V> lhs, IDictionary<K, V> rhs)
        {
            foreach (KeyValuePair<K, V> keyValue in rhs)
                lhs.Add(keyValue.Key, keyValue.Value);
        }
    }
}
