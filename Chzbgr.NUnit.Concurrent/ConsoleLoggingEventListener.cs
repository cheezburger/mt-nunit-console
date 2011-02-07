using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using NUnit.Core;

namespace Chzbgr.NUnit.Concurrent
{
    [Serializable]
    class ConsoleLoggingEventListener : EventListener, ISerializable
    {
        private readonly EventListener _wrapped;
        private readonly ThreadLocal<StringWriter> _outWriter = new ThreadLocal<StringWriter>();
        private readonly ThreadLocal<TextWriter> _oldOut = new ThreadLocal<TextWriter>();
        private readonly ThreadLocal<TextWriter> _oldErr = new ThreadLocal<TextWriter>();
        private readonly FieldInfo _fieldInfo = typeof(TestResult).GetField("message", BindingFlags.Instance | BindingFlags.NonPublic);


        public ConsoleLoggingEventListener(EventListener wrapped)
        {
            _wrapped = wrapped;
        }

        public ConsoleLoggingEventListener(SerializationInfo info, StreamingContext context)
        {
            _wrapped = (EventListener)info.GetValue("wrapped", typeof(EventListener));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("wrapped", _wrapped);
        }

        public void RunStarted(string name, int testCount)
        {
            _wrapped.RunStarted(name, testCount);
        }

        public void RunFinished(TestResult result)
        {
            _wrapped.RunFinished(result);
        }

        public void RunFinished(Exception exception)
        {
            _wrapped.RunFinished(exception);
        }

        public void TestStarted(TestName testName)
        {
            _wrapped.TestStarted(testName);

            _outWriter.Value = new StringWriter();

            _oldOut.Value = System.Console.Out;
            _oldErr.Value = System.Console.Error;

            System.Console.SetOut(_outWriter.Value);
            System.Console.SetError(_outWriter.Value);
        }

        public void TestFinished(TestResult result)
        {
            System.Console.SetOut(_oldOut.Value);
            System.Console.SetError(_oldErr.Value);

            _wrapped.TestFinished(result);

            var stringBuilder = _outWriter.Value.GetStringBuilder();
            if (stringBuilder.Length == 0)
                return;

            var msg = result.Message;
            if (!string.IsNullOrWhiteSpace(msg)) msg += Environment.NewLine + Environment.NewLine;
            msg += "Full Output:" + Environment.NewLine + stringBuilder;

            if (_fieldInfo != null)
                _fieldInfo.SetValue(result, msg);
        }

        public void SuiteStarted(TestName testName)
        {
            _wrapped.SuiteStarted(testName);
        }

        public void SuiteFinished(TestResult result)
        {
            _wrapped.SuiteFinished(result);
        }

        public void UnhandledException(Exception exception)
        {
            _wrapped.UnhandledException(exception);
        }

        public void TestOutput(TestOutput testOutput)
        {
            _wrapped.TestOutput(testOutput);
        }
    }
}