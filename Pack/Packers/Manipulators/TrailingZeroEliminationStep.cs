using System;
using System.Linq;
using Functional.Maybe;

namespace Pack.Packers.Manipulators
{
    public class TrailingZeroEliminationStep : IDataManipulationStep<int>
    {
        public Maybe<Tuple<byte[], int>> Apply(byte[] data)
        {
            var dropped = data.DropTrailingZeroes();
            return dropped.Item1 > 0
                ? Tuple.Create(dropped.Item2, dropped.Item1).ToMaybe()
                : Maybe<Tuple<byte[], int>>.Nothing;
        }

        public byte[] Reverse(byte[] data, int state)
        {
            return data.Concat(Enumerable.Repeat((byte) 0, state)).ToArray();
        }

        public byte StepIdent { get { return 2; } }

        public byte[] Reverse(byte[] data, object state)
        {
            return Reverse(data, (int) state);
        }

        public byte[] SerializeState(object state, Func<byte[]> standardFunc)
        {
            return BitConverter.GetBytes((int) state);
        }

        public object DeserializeState(byte[] stateBytes, Func<object> standardFunc)
        {
            return BitConverter.ToInt32(stateBytes, 0);
        }
    }
}