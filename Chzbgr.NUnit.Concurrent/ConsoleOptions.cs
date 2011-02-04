// ****************************************************************
// This is free software licensed under the NUnit license. You
// may obtain a copy of the license as well as information regarding
// copyright ownership at http://nunit.org.
// ****************************************************************

using System;
using System.Collections.Generic;
using Codeblast;
using NUnit.Core;

namespace Chzbgr.NUnit.Concurrent
{
    public class ConsoleOptions : Chzbgr.NUnit.Console.ConsoleOptions
    {

        [Option(Description = "The number of tests to run concurrently", Short = "dep")]
        public int degreeofparallelism;

        public ConsoleOptions(params string[] args)
            : base(args)
        {
        }

        public ConsoleOptions(bool allowForwardSlash, params string[] args)
            : base(allowForwardSlash, args)
        {
        }
    }
}
