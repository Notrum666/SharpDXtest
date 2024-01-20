using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            int size = Marshal.SizeOf(typeof(T));
            byte[] array = new byte[size];
            nint ptr = Marshal.AllocHGlobal(size);
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

        public static bool NotEmpty(this Guid guid)
        {
            return guid != Guid.Empty;
        }
    }
}