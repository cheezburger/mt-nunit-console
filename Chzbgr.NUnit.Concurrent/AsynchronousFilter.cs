using System;
using NUnit.Core;

namespace Chzbgr.NUnit.Concurrent
{
    [Serializable]
    public class AsynchronousFilter : TestFilter
    {
        private readonly int _partition;
        private readonly AsynchronousFilterState _state;

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

            var parition = _state.AssignPartition(test.TestName.UniqueName);

            return parition == _partition
                   && (0 == test.Method.GetCustomAttributes(typeof(SynchronousTestAttribute), true).Length
                       || 0 == test.Method.DeclaringType.GetCustomAttributes(typeof(SynchronousTestAttribute), true).Length);
        }
    }
}