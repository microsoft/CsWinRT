using System.IO;

namespace GuidPatch
{
    class Logger
    {
        public Logger(string parentDir)
        {
            string logFile = Path.Combine(parentDir, "log.txt"); 
            fileLogger = File.CreateText(logFile);
        }
        public Logger(string parentDir, string fileName)
        {
            string logFile = Path.Combine(parentDir, fileName); 
            fileLogger = File.CreateText(logFile);
        }

        public void Log(string text)
        {
            fileLogger?.WriteLine(text);
        }

        public void TLog(string text)
        {
            fileLogger?.WriteLine("\t" + text);
        }

        public void Close()
        {
            fileLogger?.Close();
        }

        private readonly TextWriter fileLogger;
    }

}
