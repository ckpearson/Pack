using System;
using System.Collections.Generic;
using System.Linq;

namespace Pack_v2.Tools
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

        public static IEnumerable<TResult> SelectWithPrevious<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> singlePrepare,
            Func<TResult, TSource, TResult> selector)
        {
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                {
                    yield break;
                }
                var previous = singlePrepare(iterator.Current);
                yield return previous;
                while (iterator.MoveNext())
                {
                    previous = selector(previous, iterator.Current);
                    yield return previous;
                }
            }
        }

        public static IEnumerable<Tuple<T, int>> JoinRepeatedValues<T>(this IEnumerable<T> source, int minRepeats = 2)
        {
            var lastValue = default(T);
            var hasFirstValue = false;
            var repeatCount = 0;

            foreach (var item in source)
            {
                if (!hasFirstValue)
                {
                    lastValue = item;
                    repeatCount = 1;
                    hasFirstValue = true;
                    continue;
                }

                if (!item.Equals(lastValue))
                {
                    var itemOut = lastValue;
                    var repeatOut = repeatCount;
                    if (repeatCount < minRepeats)
                    {
                        lastValue = item;
                        repeatCount = 1;
                        for(var x = 0; x < repeatOut; x++)
                        {
                            yield return Tuple.Create(itemOut, 1);
                        }
                        continue;
                    }

                    lastValue = item;
                    repeatCount = 1;
                    yield return Tuple.Create(itemOut, repeatOut);
                    continue;
                }

                repeatCount++;
            }

            if (repeatCount <= minRepeats)
            {
                for (var x = 0; x < repeatCount; x++)
                {
                    yield return Tuple.Create(lastValue, 1);
                }
            }
            else
            {
                yield return Tuple.Create(lastValue, repeatCount);
            }
        }
    }
}