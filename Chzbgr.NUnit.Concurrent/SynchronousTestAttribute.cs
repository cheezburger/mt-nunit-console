using System;

namespace Chzbgr.NUnit.Concurrent
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SynchronousTestAttribute : Attribute
    {
    }
}
