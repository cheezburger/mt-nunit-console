﻿// ****************************************************************
// Copyright 2011, Cheezburger, Inc.
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using NUnit.Core;

namespace Chzbgr.NUnit.Concurrent
{
    [Serializable]
    internal class ConsoleLoggingEventListener : EventListener, ISerializable
    {
        private readonly FieldInfo _fieldInfo = typeof(TestResult).GetField("message", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly ThreadLocal<StringWriter> _outWriter = new ThreadLocal<StringWriter>();
        private readonly EventListener _wrapped;

        public ConsoleLoggingEventListener(EventListener wrapped)
        {
            _wrapped = wrapped;
        }

        public ConsoleLoggingEventListener(SerializationInfo info, StreamingContext context)
        {
            _wrapped = (EventListener)info.GetValue("wrapped", typeof(EventListener));
        }

        #region EventListener Members

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

            //TestExecutionContext.Save();
            //TestExecutionContext.CurrentContext.Out = _outWriter.Value;
            //TestExecutionContext.CurrentContext.Error = _outWriter.Value;
        }

        public void TestFinished(TestResult result)
        {
            //TestExecutionContext.Restore();

            _wrapped.TestFinished(result);

            var stringBuilder = _outWriter.Value.GetStringBuilder();
            _outWriter.Value = null;
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
            if (_outWriter.Value != null)
                _outWriter.Value.Write(testOutput.Text);
            else
                _wrapped.TestOutput(testOutput);
        }

        #endregion

        #region ISerializable Members

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("wrapped", _wrapped);
        }

        #endregion
    }
}
