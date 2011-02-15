// ****************************************************************
// Copyright 2011, Cheezburger, Inc.
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************
using System;
using System.Collections.Generic;
using NUnit.Core;

namespace Chzbgr.NUnit.Concurrent
{
    [Serializable]
    public class SimpleNameFilter : TestFilter
    {
        private readonly HashSet<string> _fullnames;

        public SimpleNameFilter(IEnumerable<string> fullnames)
        {
            _fullnames = new HashSet<string>(fullnames);
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

            return _fullnames.Contains(itest.TestName.FullName);
        }
    }
}
