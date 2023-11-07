using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public static class Extentions
    {
        public static void AddRange<K, V>(this IDictionary<K, V> lhs, IDictionary<K, V> rhs)
        {
            foreach (KeyValuePair<K, V> keyValue in rhs)
                lhs.Add(keyValue.Key, keyValue.Value);
        }
        public static byte[] GetBytes<T>(this T obj) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);
            return array;
        }
        public static T To<T>(this byte[] data, int offset = 0) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            nint buffer = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, offset, buffer, size);
            T obj = Marshal.PtrToStructure<T>(buffer);
            Marshal.FreeHGlobal(buffer);
            return obj;
        }

    }
}
