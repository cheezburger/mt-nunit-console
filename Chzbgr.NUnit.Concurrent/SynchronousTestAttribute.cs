// ****************************************************************
// Copyright 2011, Cheezburger, Inc.
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************
using System;
using System.Collections.Generic;

namespace Chzbgr.NUnit.Concurrent
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SynchronousTestAttribute : Attribute
    {
    }
}
