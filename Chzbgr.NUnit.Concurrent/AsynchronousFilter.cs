using System;
using System.Collections.Generic;
using NUnit.Core;

namespace Chzbgr.NUnit.Concurrent
{
    [Serializable]
    public class AsynchronousFilter : TestFilter
    {
        private readonly int _partition;
        private readonly AsynchronousFilterState _state;
        private readonly Dictionary<string, int> _partitionCache = new Dictionary<string, int>();

        public AsynchronousFilter(int partition, AsynchronousFilterState state)
        {
            _partition = partition;
            _state = state;
        }

        public override bool Pass(ITest test)
        {
            return Match(test);
        }

        public override bool Match(ITest itest)
        {
            var test = itest as TestMethod;
            if (test == null)
                return true;

            var parition = AssignPartition(test);

            return parition == _partition
                   && (0 == test.Method.GetCustomAttributes(typeof(SynchronousTestAttribute), true).Length
                       || 0 == test.Method.DeclaringType.GetCustomAttributes(typeof(SynchronousTestAttribute), true).Length);
        }

        private int AssignPartition(TestMethod test)
        {
            var name = test.TestName.UniqueName;
            int partition;
            if (_partitionCache.TryGetValue(name, out partition))
                return partition;
            _partitionCache[name] = partition = _state.AssignPartition(name);
            return partition;
        }
    }
}