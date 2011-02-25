// ****************************************************************
// Copyright 2008, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Core;
using NUnit.Util;

namespace Chzbgr.NUnit.Concurrent
{
    /// <summary>
    ///   Summary description for Runner.
    /// </summary>
    public class Runner
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(Runner));

        [STAThread]
        public static int Main(string[] args)
        {
            var options = new ConsoleOptions(args);

            // Create SettingsService early so we know the trace level right at the start
            var settingsService = new SettingsService();
            var level = (InternalTraceLevel)settingsService.GetSetting("Options.InternalTraceLevel", InternalTraceLevel.Default);
            if (options.trace != InternalTraceLevel.Default)
                level = options.trace;

            InternalTrace.Initialize("nunit-console_%p.log", level);

            log.Info("NUnit-System.Console.exe starting");

            if (!options.nologo)
                WriteCopyright();

            if (options.help)
            {
                options.Help();
                return ConsoleUi.OK;
            }

            if (options.NoArgs)
            {
                System.Console.Error.WriteLine("fatal error: no inputs specified");
                options.Help();
                return ConsoleUi.OK;
            }

            if (!options.Validate())
            {
                foreach (string arg in options.InvalidArguments)
                    System.Console.Error.WriteLine("fatal error: invalid argument: {0}", arg);
                options.Help();
                return ConsoleUi.INVALID_ARG;
            }

            // Add Standard Services to ServiceManager
            ServiceManager.Services.AddService(settingsService);
            ServiceManager.Services.AddService(new DomainManager());
            //ServiceManager.Services.AddService( new RecentFilesService() );
            ServiceManager.Services.AddService(new ProjectService());
            //ServiceManager.Services.AddService( new TestLoader() );
            ServiceManager.Services.AddService(new AddinRegistry());
            ServiceManager.Services.AddService(new AddinManager());
            ServiceManager.Services.AddService(new TestAgency());

            // Initialize Services
            ServiceManager.Services.InitializeServices();

            foreach (string parm in options.Parameters)
            {
                if (!Services.ProjectService.CanLoadProject(parm) && !PathUtils.IsAssemblyFileType(parm))
                {
                    System.Console.WriteLine("File type not known: {0}", parm);
                    return ConsoleUi.INVALID_ARG;
                }
            }

            try
            {
                var consoleUi = new ConsoleUi();
                return consoleUi.Execute(options);
            }
            catch (FileNotFoundException ex)
            {
                System.Console.WriteLine(ex.Message);
                return ConsoleUi.FILE_NOT_FOUND;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Unhandled Exception:\n{0}", ex);
                return ConsoleUi.UNEXPECTED_ERROR;
            }
            finally
            {
                if (options.wait)
                {
                    System.Console.Out.WriteLine("\nHit <enter> key to continue");
                    System.Console.ReadLine();
                }

                log.Info("NUnit-System.Console.exe terminating");
            }
        }

        private static void WriteCopyright()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var versionText = executingAssembly.GetName().Version.ToString();

            var productName = "NUnit";
            var copyrightText = "Copyright (C) 2002-2009 Charlie Poole.\r\nCopyright (C) 2002-2004 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov.\r\nCopyright (C) 2000-2002 Philip Craig.\r\nCopyright (C) 2011 Cheezburger, Inc.\r\nAll Rights Reserved.";

            var objectAttrs = executingAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (objectAttrs.Length > 0)
                productName = ((AssemblyProductAttribute)objectAttrs[0]).Product;

            objectAttrs = executingAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if (objectAttrs.Length > 0)
                copyrightText = ((AssemblyCopyrightAttribute)objectAttrs[0]).Copyright;

            objectAttrs = executingAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
            if (objectAttrs.Length > 0)
            {
                var configText = ((AssemblyConfigurationAttribute)objectAttrs[0]).Configuration;
                if (configText != "")
                    versionText += string.Format(" ({0})", configText);
            }

            System.Console.WriteLine(String.Format("{0} version {1}", productName, versionText));
            System.Console.WriteLine(copyrightText);
            System.Console.WriteLine();

            System.Console.WriteLine("Runtime Environment - ");
            var framework = RuntimeFramework.CurrentFramework;
            System.Console.WriteLine(string.Format("   OS Version: {0}", Environment.OSVersion));
            System.Console.WriteLine(string.Format("  CLR Version: {0} ( {1} )",
                Environment.Version, framework.DisplayName));
            System.Console.WriteLine(string.Format("      Runtime: {0} bit", Environment.Is64BitProcess ? "64" : "32"));

            System.Console.WriteLine();
        }
    }
}
