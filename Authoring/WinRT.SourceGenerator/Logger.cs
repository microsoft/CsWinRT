using Microsoft.CodeAnalysis;
using System.IO;

namespace Generator
{
    class Logger
    {
        public static void Initialize(SourceGeneratorContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTEnableLogging", out var enableLogging);
            if(enableLogging != null && bool.Parse(enableLogging))
            {
                string logFile = Path.Combine(SourceGenerator.GetGeneratedFilesDir(context), "log.txt");
                fileLogger = File.CreateText(logFile);
            }
        }

        public static void Log(string text)
        {
            fileLogger?.WriteLine(text);
        }

        public static void Close()
        {
            fileLogger?.Close();
        }

        private static TextWriter fileLogger;
    }
}
