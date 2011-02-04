using System;
using NUnit.Core;

namespace Chzbgr.NUnit.Concurrent
{
    [Serializable]
    public class SynchronousFilter : TestFilter
    {
        public override bool Pass(ITest test)
        {
            return Match(test);
        }

        public override bool Match(ITest itest)
        {
            var test = itest as TestMethod;
            if (test == null)
                return true;

            var result = 0 != test.Method.GetCustomAttributes(typeof(SynchronousTestAttribute), true).Length
                         || 0 != test.Method.DeclaringType.GetCustomAttributes(typeof(SynchronousTestAttribute), true).Length;

            if (result)
                System.Console.WriteLine("Running {0} Synchronously", test.TestName.FullName);

            return result;
        }
    }
}