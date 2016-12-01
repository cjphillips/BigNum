using System;
using System.Collections;

namespace Phillips
{
    internal static class Extensions
    {
        public static void ThrowIfNullArg(this object value)
        {
            if (null == value)
            {
                throw new ArgumentNullException();
            }
        }

        public static void ThrowIfUndefined(this BigNum value)
        {
            value.ThrowIfNullArg();

            if (value.IsUndefined)
            {
                throw new InvalidOperationException("Value is undefined.");
            }
        }

        public static void Reverse(this BitArray array)
        {
            int midPoint = array.Length/2;

            for (int i = 0; i < midPoint; i++)
            {
                bool bit = array[i];
                array[i] = array[array.Length - i - 1];
                array[array.Length - i - 1] = bit;
            }
        }
    }
}
