// ****************************************************************
// This is free software licensed under the NUnit license. You
// may obtain a copy of the license as well as information regarding
// copyright ownership at http://nunit.org.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Chzbgr.NUnit.Console;
using NUnit.Core;
using NUnit.Core.Filters;
using NUnit.Util;

namespace Chzbgr.NUnit.Concurrent
{
    /// <summary>
    ///   Summary description for ConsoleUi.
    /// </summary>
    class ConsoleUi : Console.ConsoleUi
    {
        protected override int DoRun(Console.ConsoleOptions options, bool redirectOutput, bool redirectError, TestPackage package, TextWriter outWriter, TextWriter errorWriter, TestFilter testFilter, out TestResult result, EventCollector collector)
        {
            result = null;
            var testRunner1 = new DefaultTestRunnerFactory().MakeTestRunner(package);
            try
            {
                testRunner1.Load(package);

                if (testRunner1.Test == null)
                {
                    testRunner1.Unload();
                    System.Console.Error.WriteLine("Unable to locate fixture {0}", options.fixture);
                    return FIXTURE_NOT_FOUND;
                }
            }
            finally
            {
                var disp = testRunner1 as IDisposable;
                if (disp != null)
                    disp.Dispose();
            }

            result = new TestResult(new TestName { Name = "Global" });
            var timer = new Stopwatch();
            timer.Start();

            var testRunnerId = 0;
            {
                var syncTestRunner = new DefaultTestRunnerFactory().MakeTestRunner(package);
                var syncFilter = new AndFilter(testFilter, new SynchronousFilter());
                result.AddResult(RunPartition(redirectOutput, redirectError, package, outWriter, errorWriter, syncFilter, syncTestRunner, collector));
                testRunnerId = syncTestRunner.ID;
            }

            var dep = 0;
            var consoleOptions = options as ConsoleOptions;
            if (options != null)
                dep = consoleOptions.degreeofparallelism;
            if (dep == 0)
                dep = 4;
            System.Console.WriteLine("Degree of Parallelism: {0}", dep);

            var state = new AsynchronousFilterState(dep);
            var asyncTestRunner = new ParallelTestRunner(testRunnerId + 1, dep, state);
            result.AddResult(RunPartition(redirectOutput, redirectError, package, outWriter, errorWriter, testFilter, asyncTestRunner, collector));

            if (consoleOptions != null && consoleOptions.retestfailures)
            {
                var failedTestFilter = new SimpleNameFilter(from test in Flatten(result)
                                                            where test.IsFailure
                                                            select test.FullName);

                var retestTestRunner = new DefaultTestRunnerFactory().MakeTestRunner(package);
                var retestFilter = new AndFilter(testFilter, failedTestFilter);
                result.AddResult(RunPartition(redirectOutput, redirectError, package, outWriter, errorWriter, retestFilter, retestTestRunner, collector));

            }

            timer.Stop();
            result.Time = timer.ElapsedTicks / (double)TimeSpan.TicksPerSecond;
            return 0;
        }

        public IEnumerable<TestResult> Flatten(TestResult src)
        {
            var results = new Stack<TestResult>();
            results.Push(src);

            while (results.Count > 0)
            {
                var result = results.Pop();
                if (result.HasResults)
                    foreach (TestResult child in result.Results)
                        results.Push(child);
                else
                    yield return result;
            }
        }

        private TestResult RunPartition(bool redirectOutput, bool redirectError, TestPackage package, TextWriter outWriter, TextWriter errorWriter, TestFilter testFilter, TestRunner testRunner, EventCollector collector)
        {
            TestResult result;
            try
            {
                testRunner.Load(package);
                result = RunPartition(redirectOutput, redirectError, testRunner, outWriter, errorWriter, testFilter, collector);
            }
            finally
            {
                var disp = testRunner as IDisposable;
                if (disp != null)
                    disp.Dispose();
            }
            return result;
        }

        private TestResult RunPartition(bool redirectOutput, bool redirectError, TestRunner testRunner, TextWriter outWriter, TextWriter errorWriter, TestFilter testFilter, EventCollector collector)
        {
            TestResult result = null;
            var savedDirectory = Environment.CurrentDirectory;
            var savedOut = System.Console.Out;
            var savedError = System.Console.Error;

            try
            {
                result = testRunner.Run(collector, testFilter);
            }
            finally
            {
                outWriter.Flush();
                errorWriter.Flush();

                if (redirectOutput)
                    outWriter.Close();
                if (redirectError)
                    errorWriter.Close();

                Environment.CurrentDirectory = savedDirectory;
                System.Console.SetOut(savedOut);
                System.Console.SetError(savedError);
            }

            System.Console.WriteLine();
            return result;
        }
    }
}
