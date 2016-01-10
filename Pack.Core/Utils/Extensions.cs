using System;
using System.Collections.Generic;
using System.Linq;

namespace Pack.Core.Utils
{
    public static class Extensions
    {
        public static void SetIfInIndex<T>(this IList<T> list, int index, Func<T> valueFunc)
        {
            if (index >= list.Count) return;
            list[index] = valueFunc();
        }

        public static void SetIfInIndex<T>(this IList<T> list, int index, T value)
        {
            SetIfInIndex(list, index, () => value);
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