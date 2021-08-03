using System;
using System.IO;

namespace JetBrains.Refasmer
{
    public class VerySimpleLogger: ILogger
    {
        private readonly TextWriter _writer;
        private readonly LogLevel _level;

        public VerySimpleLogger(Stream stream, LogLevel level = LogLevel.Trace)
        {
            _level = level;
            _writer = new StreamWriter(stream);
        }

        public VerySimpleLogger(TextWriter writer, LogLevel level = LogLevel.Trace)
        {
            _writer = writer;
            _level = level;

        }
        
        public void Log(LogLevel logLevel, string message)
        {
            if (IsEnabled(logLevel))
                _writer.WriteLine(message);
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _level;
    }
}