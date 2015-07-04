using System;
using System.Collections.Generic;
using System.Linq;
using Functional.Maybe;

namespace Pack.Packers.Manipulators
{
    /*
     * 4 bytes - start address of collapse
     * 1 byte - value collapsed
     * 1 byte - size of collapse
     */
    public class ContiguousRangeCollapseStep : IDataManipulationStep<IEnumerable<ContiguousRangeCollapseStep.CollapseDetails>>
    {
        public class CollapseDetails : IEquatable<CollapseDetails>
        {
            private readonly int _address;
            private readonly byte _value;
            private readonly byte _size;

            public CollapseDetails(int address, byte value, byte size)
            {
                _address = address;
                _value = value;
                _size = size;
            }

            public int Address
            {
                get { return _address; }
            }

            public byte Value
            {
                get { return _value; }
            }

            public byte Size
            {
                get { return _size; }
            }

            public bool Equals(CollapseDetails other)
            {
                if (other == null) return false;
                return other.Address == Address &&
                       other.Size == Size &&
                       other.Value == Value;
            }
        }

        public byte StepIdent { get { return 3; } }

        public byte[] Reverse(byte[] data, object state)
        {
            return Reverse(data, (IEnumerable<CollapseDetails>)state);
        }

        public byte[] SerializeState(object state, Func<byte[]> standardFunc)
        {
            var dets = (IEnumerable<CollapseDetails>)state;
            return dets.Aggregate(new byte[] { },
                (agg, d) => agg.Concat(BitConverter.GetBytes(d.Address).Concat(new[] { d.Value, d.Size })).ToArray());
        }

        public object DeserializeState(byte[] stateBytes, Func<object> standardFunc)
        {
            return stateBytes.Select((b, idx) => new { b, idx = idx / 6 })
                .GroupBy(g => g.idx, g => g.b)
                .Select(g => g.ToArray())
                .Select(arr => new CollapseDetails(BitConverter.ToInt32(arr, 0),
                    arr[4], arr[5]))
                .ToList();
        }

        public Maybe<Tuple<byte[], IEnumerable<CollapseDetails>>> Apply(byte[] data)
        {
            var repeats =
                data.JoinRepeatedValues(7)
                    .SelectMany(x =>
                    {
                        if (x.Item2 == 1)
                        {
                            return new[]
                            {
                                new
                                {
                                    Data = x.Item1,
                                    Size = 1,
                                    Coll = false,
                                }
                            };
                        }

                        var chunks = (double) x.Item2/255;
                        var whole = (int) Math.Truncate(chunks);
                        var partialRemaining = x.Item2 - (whole*255);

                        return Enumerable.Range(1, whole)
                            .Select(_ => new
                            {
                                Data = x.Item1,
                                Size = 255,
                                Coll = true,
                            })
                            .Concat(partialRemaining < 7
                                ? Enumerable.Repeat(x.Item1, partialRemaining)
                                    .Select(_ => new {Data = x.Item1, Size = 1, Coll = false})
                                : new[]
                                {
                                    new
                                    {
                                        Data = x.Item1,
                                        Size = partialRemaining,
                                        Coll = true
                                    }
                                });
                    })
                    .SelectWithPrevious(x => new {x.Data, x.Size, x.Coll, Index = 0},
                        (prev, curr) => new {curr.Data, curr.Size, curr.Coll, Index = (prev.Index + prev.Size)});

            var res = repeats
                .Aggregate(new List<Tuple<byte, CollapseDetails>>(),
                    (agg, item) =>
                    {
                        agg.Add(Tuple.Create(item.Data,
                            !item.Coll
                                ? null
                                : new CollapseDetails(item.Index, item.Data, (byte) item.Size))
                            );
                        return agg;
                    });

            var bytes = res.Where(t => t.Item2 == null).Select(t => t.Item1).ToArray();
            var collapses = res.Select(t => t.Item2).Where(x => x != null).ToList();

            return collapses.Count == 0
                ? Maybe<Tuple<byte[], IEnumerable<CollapseDetails>>>.Nothing
                : Tuple.Create(bytes, collapses.AsEnumerable()).ToMaybe();
        }

        //public Maybe<Tuple<byte[], IEnumerable<CollapseDetails>>> ApplyOld(byte[] data)
        //{
        //    var x =
        //        data
        //            .JoinRepeatedValuesA()
        //            .SelectMany(items =>
        //            {
        //                var arr = items.ToArray();
        //                if (arr.Length <= 6)
        //                {
        //                    return new[]
        //                    {
        //                        new
        //                        {
        //                            Data = arr,
        //                            Size = arr.Length,
        //                            Collapse = false,
        //                        }
        //                    };
        //                }

        //                var chunks =
        //                    arr.Select((b, iidx) => new { b, idx = iidx / 255 })
        //                        .GroupBy(xi => xi.idx, xi => xi.b)
        //                        .Select((g, iidx) => new { idx = iidx, data = g.ToArray() })
        //                        .ToList();

        //                return chunks
        //                    .Select(c => new
        //                    {
        //                        Data = new[] { c.data[0] },
        //                        Size = c.data.Length,
        //                        Collapse = true
        //                    });
        //            });

        //    var sane =
        //        x.SelectPairwise(
        //            (item, idx) => new
        //            {
        //                item.Collapse,
        //                item.Data,
        //                item.Size,
        //                idx,
        //            },
        //            (prev, curr, idx) =>
        //            {
        //                if (idx == 1)
        //                {
        //                    prev = new
        //                    {
        //                        Collapse = prev.Collapse,
        //                        Data = prev.Data,
        //                        Size = prev.Size,
        //                        idx = 0,
        //                    };
        //                }

        //                curr = new
        //                {
        //                    Collapse = curr.Collapse,
        //                    Data = curr.Data,
        //                    Size = curr.Size,
        //                    idx = (prev.idx + prev.Size),
        //                };

        //                return Tuple.Create(prev, curr);
        //            });

        //    var final =
        //        sane.Aggregate(new Tuple<byte[], List<CollapseDetails>>(new byte[] { }, new List<CollapseDetails>()),
        //            (agg, item) =>
        //            {
        //                if (!item.Collapse)
        //                {
        //                    return Tuple.Create(agg.Item1.Concat(item.Data).ToArray(), agg.Item2);
        //                }

        //                return Tuple.Create(agg.Item1, agg.Item2.Concat(new[]
        //                {
        //                    new CollapseDetails(item.idx, item.Data[0], (byte) item.Size),
        //                }).ToList());
        //            });

        //    var bytes = final.Item1;
        //    var collapses = final.Item2;

        //    if (collapses.Count == 0) return Maybe<Tuple<byte[], IEnumerable<CollapseDetails>>>.Nothing;

        //    return Tuple.Create(final.Item1, final.Item2.AsEnumerable()).ToMaybe();
        //}

        public byte[] Reverse(byte[] data, IEnumerable<CollapseDetails> state)
        {
            var final = new List<byte>();
            state = state.ToList();

            var dataQueue = new Queue<byte>(data);

            var address = 0;
            do
            {
                var collapse = state.SingleOrDefault(c => c.Address == address);
                if (collapse == null)
                {
                    final.Add(dataQueue.Dequeue());
                    address++;
                    continue;
                }

                final.AddRange(Enumerable.Repeat(collapse.Value, collapse.Size));
                address += collapse.Size;
            } while (dataQueue.Count > 0);

            return final.ToArray();
        }
    }
}