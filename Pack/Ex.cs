using System;
using System.IO;
using System.Linq;
using SevenZip;

namespace Pack
{
    public static class Ex
    {
        public static void SetIfInIndex<T>(this T[] arr, int index, Func<T> value)
        {
            if (index >= arr.Length) return;
            arr[index] = value();
        }

        public static void SetIfInIndex<T>(this T[] arr, int index, T value)
        {
            arr.SetIfInIndex(index, () => value);
        }

        public static Tuple<int, byte[]> DropTrailingZeroes(this byte[] bytes)
        {
            var count = 0;
            var length = bytes.Length;
            for (var i = length - 1; i > 0; i--)
            {
                if (bytes[i] == 0)
                {
                    count++;
                    continue;
                }
                break;
            }
            return Tuple.Create(count, bytes.Take(length - count).ToArray());
        }
    }
}