using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chzbgr.NUnit.Console;
using NUnit.Core;
using NUnit.Core.Filters;

namespace Chzbgr.NUnit.Concurrent
{
    class ParallelTestRunner : MultipleTestDomainRunner
    {
        private readonly AsynchronousFilterState _state;
        private readonly int _degreeOfParallelism;

        public ParallelTestRunner(int degreeOfParallelism, AsynchronousFilterState state)
            : this(1, degreeOfParallelism, state)
        {
        }

        public ParallelTestRunner(int id, int degreeOfParallelism, AsynchronousFilterState state)
            : base(id)
        {
            _degreeOfParallelism = degreeOfParallelism;
            _state = state;
        }

        protected override void LoadRunnders(TestPackage package, string targetAssemblyName, ref int nfound)
        {
            for (int i = 0; i < _degreeOfParallelism; i++)
            {
                var testRunner = CreateRunner(base.ID * 100 + i + 1);
                testRunner.Load(package);
                runners.Add(testRunner);
                nfound++;
            }
        }

        protected override TestResult InternalRun(ITestFilter filter, TestResult result)
        {
            for (int i = 0; i < runners.Count; i++)
            {
                var runner = (TestRunner)runners[i];
                runner.BeginRun(this, new AndFilter(filter, new AsynchronousFilter(i, _state)));
            }

            return EndRun();
        }
    }
}
