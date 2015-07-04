using System;
using System.Collections.Generic;
using System.Linq;

namespace Pack.Packers.Manipulators
{
    public class ManipulationResult : IEquatable<ManipulationResult>
    {
        private readonly byte[] _currentData;
        private readonly IDictionary<IDataManipulationStep, object> _stepsApplied;

        public ManipulationResult(byte[] currentData,
            IDictionary<IDataManipulationStep, object> stepsApplied)
        {
            _currentData = currentData;
            _stepsApplied = stepsApplied;
        }

        public byte[] CurrentData
        {
            get { return _currentData; }
        }

        public IDictionary<IDataManipulationStep, object> StepsApplied
        {
            get { return _stepsApplied; }
        }

        public ManipulationResult Append(ManipulationResult result)
        {
            return new ManipulationResult(result._currentData,
                _stepsApplied.Concat(result._stepsApplied).ToDictionary(k => k.Key, k => k.Value));
        }

        public bool Equals(ManipulationResult other)
        {
            return _currentData.Equals(other.CurrentData) &&
                   _stepsApplied.SequenceEqual(other._stepsApplied);
        }
    }
}