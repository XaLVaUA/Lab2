using System;
using System.IO;

namespace Lab2
{
    public sealed class Logger : IDisposable
    {
        private readonly StreamWriter _writer;

        public Logger(string logFilePath)
        {
            _writer = File.AppendText(logFilePath);
            _writer.AutoFlush = true;
        }

        public void LogLine(string text)
        {
            _writer.WriteLine(text);
        }

        public void LogLine()
        {
            _writer.WriteLine();
        }

        public void Dispose()
        {
            // ReSharper disable once InvertIf
            if (_writer is not null)
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
            }
        }
    }
}