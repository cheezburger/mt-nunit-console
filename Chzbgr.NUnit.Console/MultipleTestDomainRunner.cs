// ****************************************************************
// This is free software licensed under the NUnit license. You
// may obtain a copy of the license as well as information regarding
// copyright ownership at http://nunit.org.
// ****************************************************************
using NUnit.Core;
using NUnit.Util;

namespace Chzbgr.NUnit.Console
{
    /// <summary>
    /// Summary description for MultipleTestDomainRunner.
    /// </summary>
    public class MultipleTestDomainRunner : AggregatingTestRunner
    {
        #region Constructors
        public MultipleTestDomainRunner() : base(0) { }

        public MultipleTestDomainRunner(int runnerId) : base(runnerId) { }
        #endregion

        #region CreateRunner
        protected override TestRunner CreateRunner(int runnerId)
        {
            return new TestDomain(runnerId);
        }
        #endregion
    }
}