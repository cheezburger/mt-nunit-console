using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return this.Match(test);
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
