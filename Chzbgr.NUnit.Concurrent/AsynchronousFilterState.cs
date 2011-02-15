// ****************************************************************
// Copyright 2011, Cheezburger, Inc.
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************
using System;
using System.Collections.Generic;

namespace Chzbgr.NUnit.Concurrent
{
    public class AsynchronousFilterState : MarshalByRefObject
    {
        private readonly int _degreeOfParallelism;
        private readonly Dictionary<string, int> _partitions = new Dictionary<string, int>();

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
                p = _partitions.Count%_degreeOfParallelism;
                _partitions[name] = p;
                return p;
            }
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
