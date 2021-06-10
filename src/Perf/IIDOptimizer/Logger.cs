using System.IO;

namespace GuidPatch
{
    class Logger
    {
        public Logger(string parentDir, string fileName)
        {
            string logFile = Path.Combine(parentDir, fileName); 
            fileLogger = File.CreateText(logFile);
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
