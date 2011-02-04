// ****************************************************************
// This is free software licensed under the NUnit license. You
// may obtain a copy of the license as well as information regarding
// copyright ownership at http://nunit.org.
// ****************************************************************

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Core;
using NUnit.Core.Filters;
using NUnit.Util;

namespace Chzbgr.NUnit.Console
{
    /// <summary>
    /// Summary description for ConsoleUi.
    /// </summary>
    public abstract class ConsoleUi
    {
        public static readonly int OK;
        public static readonly int INVALID_ARG = -1;
        public static readonly int FILE_NOT_FOUND = -2;
        public static readonly int FIXTURE_NOT_FOUND = -3;
        public static readonly int UNEXPECTED_ERROR = -100;

        public int Execute(ConsoleOptions options)
        {
            var outWriter = System.Console.Out;
            var redirectOutput = options.output != null && options.output != string.Empty;
            if (redirectOutput)
            {
                var outStreamWriter = new StreamWriter(options.output);
                outStreamWriter.AutoFlush = true;
                outWriter = outStreamWriter;
            }

            var errorWriter = System.Console.Error;
            var redirectError = options.err != null && options.err != string.Empty;
            if (redirectError)
            {
                var errorStreamWriter = new StreamWriter(options.err);
                errorStreamWriter.AutoFlush = true;
                errorWriter = errorStreamWriter;
            }

            var package = MakeTestPackage(options);

            System.Console.WriteLine("ProcessModel: {0}    DomainUsage: {1}",
                package.Settings.Contains("ProcessModel")
                    ? package.Settings["ProcessModel"]
                    : "Default",
                package.Settings.Contains("DomainUsage")
                    ? package.Settings["DomainUsage"]
                    : "Default");

            System.Console.WriteLine("Execution Runtime: {0}",
                package.Settings.Contains("RuntimeFramework")
                    ? package.Settings["RuntimeFramework"]
                    : "Default");

            var collector = new EventCollector(options, outWriter, errorWriter);

            var testFilter = TestFilter.Empty;
            if (options.run != null && options.run != string.Empty)
            {
                System.Console.WriteLine("Selected test(s): " + options.run);
                testFilter = new SimpleNameFilter(options.run);
            }

            if (options.include != null && options.include != string.Empty)
            {
                System.Console.WriteLine("Included categories: " + options.include);
                var includeFilter = new CategoryExpression(options.include).Filter;
                if (testFilter.IsEmpty)
                    testFilter = includeFilter;
                else
                    testFilter = new AndFilter(testFilter, includeFilter);
            }

            if (options.exclude != null && options.exclude != string.Empty)
            {
                System.Console.WriteLine("Excluded categories: " + options.exclude);
                TestFilter excludeFilter = new NotFilter(new CategoryExpression(options.exclude).Filter);
                if (testFilter.IsEmpty)
                    testFilter = excludeFilter;
                else if (testFilter is AndFilter)
                    ((AndFilter)testFilter).Add(excludeFilter);
                else
                    testFilter = new AndFilter(testFilter, excludeFilter);
            }

            if (testFilter is NotFilter)
                ((NotFilter)testFilter).TopLevel = true;
            TestResult result;

            var returnCode = DoRun(options, redirectOutput, redirectError, package, outWriter, errorWriter, testFilter, out result, collector);
            if (returnCode != 0)
                return returnCode;

            returnCode = UNEXPECTED_ERROR;

            if (result != null)
            {
                var xmlOutput = CreateXmlOutput(result);
                var summary = new ResultSummarizer(result);

                if (options.xmlConsole)
                {
                    System.Console.WriteLine(xmlOutput);
                }
                else
                {
                    WriteSummaryReport(summary);
                    if (summary.ErrorsAndFailures > 0)
                        WriteErrorsAndFailuresReport(result);
                    if (summary.TestsNotRun > 0)
                        WriteNotRunReport(result);
                }

                // Write xml output here
                var xmlResultFile = options.xml == null || options.xml == string.Empty
                                        ? "TestResult.xml" : options.xml;

                using (var writer = new StreamWriter(xmlResultFile))
                {
                    writer.Write(xmlOutput);
                }

                returnCode = summary.ErrorsAndFailures;
            }

            if (collector.HasExceptions)
            {
                collector.WriteExceptions();
                returnCode = UNEXPECTED_ERROR;
            }

            return returnCode;

        }

        protected abstract int DoRun(ConsoleOptions options, bool redirectOutput, bool redirectError, TestPackage package, TextWriter outWriter, TextWriter errorWriter, TestFilter testFilter, out TestResult result, EventCollector collector);

        #region Helper Methods

        // TODO: See if this can be unified with the Gui's MakeTestPackage
        private int reportIndex;

        private static TestPackage MakeTestPackage(ConsoleOptions options)
        {
            TestPackage package;
            var domainUsage = DomainUsage.Default;
            var processModel = ProcessModel.Default;
            RuntimeFramework framework = null;

            var parameters = new string[options.ParameterCount];
            for (var i = 0; i < options.ParameterCount; i++)
                parameters[i] = Path.GetFullPath((string)options.Parameters[i]);

            if (options.IsTestProject)
            {
                var project =
                    Services.ProjectService.LoadProject(parameters[0]);

                var configName = options.config;
                if (configName != null)
                    project.SetActiveConfig(configName);

                package = project.ActiveConfig.MakeTestPackage();
                processModel = project.ProcessModel;
                domainUsage = project.DomainUsage;
                framework = project.ActiveConfig.RuntimeFramework;
            }
            else if (parameters.Length == 1)
            {
                package = new TestPackage(parameters[0]);
                domainUsage = DomainUsage.Single;
            }
            else
            {
                // TODO: Figure out a better way to handle "anonymous" packages
                package = new TestPackage(null, parameters);
                package.AutoBinPath = true;
                domainUsage = DomainUsage.Multiple;
            }

            if (options.process != ProcessModel.Default)
                processModel = options.process;

            if (options.domain != DomainUsage.Default)
                domainUsage = options.domain;

            if (options.framework != null)
                framework = RuntimeFramework.Parse(options.framework);

            package.TestName = options.fixture;

            package.Settings["ProcessModel"] = processModel;
            package.Settings["DomainUsage"] = domainUsage;
            if (framework != null)
                package.Settings["RuntimeFramework"] = framework;


            if (domainUsage == DomainUsage.None)
            {
                // Make sure that addins are available
                CoreExtensions.Host.AddinRegistry = Services.AddinRegistry;
            }

            package.Settings["ShadowCopyFiles"] = !options.noshadow;
            package.Settings["UseThreadedRunner"] = !options.nothread;
            package.Settings["DefaultTimeout"] = options.timeout;

            return package;
        }

        private static string CreateXmlOutput(TestResult result)
        {
            var builder = new StringBuilder();
            new XmlResultWriter(new StringWriter(builder)).SaveTestResult(result);

            return builder.ToString();
        }

        private static void WriteSummaryReport(ResultSummarizer summary)
        {
            System.Console.WriteLine(
                "Tests run: {0}, Errors: {1}, Failures: {2}, Inconclusive: {3}, Time: {4} seconds",
                summary.TestsRun, summary.Errors, summary.Failures, summary.Inconclusive, summary.Time);
            System.Console.WriteLine(
                "  Not run: {0}, Invalid: {1}, Ignored: {2}, Skipped: {3}",
                summary.TestsNotRun, summary.NotRunnable, summary.Ignored, summary.Skipped);
            System.Console.WriteLine();
        }

        private void WriteErrorsAndFailuresReport(TestResult result)
        {
            reportIndex = 0;
            System.Console.WriteLine("Errors and Failures:");
            WriteErrorsAndFailures(result);
            System.Console.WriteLine();
        }

        private void WriteErrorsAndFailures(TestResult result)
        {
            if (result.Executed)
            {
                if (result.HasResults)
                {
                    if ((result.IsFailure || result.IsError) && result.FailureSite == FailureSite.SetUp)
                        WriteSingleResult(result);

                    foreach (TestResult childResult in result.Results)
                        WriteErrorsAndFailures(childResult);
                }
                else if (result.IsFailure || result.IsError)
                {
                    WriteSingleResult(result);
                }
            }
        }

        private void WriteNotRunReport(TestResult result)
        {
            reportIndex = 0;
            System.Console.WriteLine("Tests Not Run:");
            WriteNotRunResults(result);
            System.Console.WriteLine();
        }

        private void WriteNotRunResults(TestResult result)
        {
            if (result.HasResults)
                foreach (TestResult childResult in result.Results)
                    WriteNotRunResults(childResult);
            else if (!result.Executed)
                WriteSingleResult(result);
        }

        private void WriteSingleResult(TestResult result)
        {
            var status = result.IsFailure || result.IsError
                             ? string.Format("{0} {1}", result.FailureSite, result.ResultState)
                             : result.ResultState.ToString();

            System.Console.WriteLine("{0}) {1} : {2}", ++reportIndex, status, result.FullName);

            if (result.Message != null && result.Message != string.Empty)
                System.Console.WriteLine("   {0}", result.Message);

            if (result.StackTrace != null && result.StackTrace != string.Empty)
                System.Console.WriteLine(result.IsFailure
                                      ? StackTraceFilter.Filter(result.StackTrace)
                                      : result.StackTrace + Environment.NewLine);
        }

        #endregion
    }
}

