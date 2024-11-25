using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Generator
{
    class Logger
    {
        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035", Justification = "We need to do file IO to save the 'cswinrt' log file.")]
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
