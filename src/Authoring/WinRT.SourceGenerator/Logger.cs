﻿using Microsoft.CodeAnalysis;
using System.IO;

namespace Generator
{
    class Logger
    {
        public Logger(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTEnableLogging", out var enableLoggingStr);
            if (enableLoggingStr != null && bool.TryParse(enableLoggingStr, out var enableLogging) && enableLogging)
            {
                string logFile = Path.Combine(context.GetGeneratedFilesDir(), "log.txt");
                fileLogger = File.CreateText(logFile);
            }
        }

        public void Log(string text)
        {
            fileLogger?.WriteLine(text);
        }

        public void Close()
        {
            fileLogger?.Close();
        }

        private readonly TextWriter fileLogger;
    }
}
