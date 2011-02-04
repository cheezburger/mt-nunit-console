using System;
using System.Collections.Generic;

namespace Chzbgr.NUnit.Concurrent
{
    public class AsynchronousFilterState : MarshalByRefObject
    {
        private readonly Dictionary<string, int> _partitions = new Dictionary<string, int>();
        private readonly int _degreeOfParallelism;

        public AsynchronousFilterState(int degreeOfParallelism)
        {
            _degreeOfParallelism = degreeOfParallelism;
        }

        public int AssignPartition(string name)
        {
            lock (_partitions)
            {
                int p;
                if (_partitions.TryGetValue(name, out p))
                    return p;
                p = _partitions.Count % _degreeOfParallelism;
                _partitions[name] = p;
                return p;
            }
        }
    }
}