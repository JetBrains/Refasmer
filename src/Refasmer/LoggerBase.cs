using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace JetBrains.Refasmer
{
    public class LoggerBase
    {
        private readonly Stack<string> _loggerPrefixStack = new Stack<string>(new[] {""});

        private readonly ILogger _logger;

        private struct LogPrefix : IDisposable
        {
            private readonly string _prefix;
            private readonly Stack<string> _loggerPrefixStack;

            public LogPrefix(string prefix, Stack<string> loggerPrefixStack)
            {
                _loggerPrefixStack = loggerPrefixStack;

                _prefix = $"{_loggerPrefixStack.Peek()}  {prefix}";
                _loggerPrefixStack.Push(_prefix);
            }

            public void Dispose()
            {
                if (_loggerPrefixStack.Peek() == _prefix)
                    _loggerPrefixStack.Pop();
                else
                    throw new Exception("Logger prefix was modified");
            }
        }

        public IDisposable WithLogPrefix(string prefix) => new LogPrefix(prefix, _loggerPrefixStack);
        
        public LoggerBase(ILogger logger)
        {
            _logger = logger;
        }

        public LoggerBase(LoggerBase loggerBase)
        {
            _logger = loggerBase._logger;

            foreach (var prefix in loggerBase._loggerPrefixStack.Reverse().ToList())
            {
                _loggerPrefixStack.Push(prefix);                
            }
        }

        protected void PushLogPrefix(string prefix)
        {
            _loggerPrefixStack.Push($"{_loggerPrefixStack.Peek()}  {prefix}");
        }

        protected string PopLogPrefix()
        {
            return _loggerPrefixStack.Pop();
        }

        public void Info( string msg )
        {
            _logger.LogInformation($"{_loggerPrefixStack.Peek()} {msg}");
        }

        public void Debug( string msg )
        {
            _logger.LogDebug($"{_loggerPrefixStack.Peek()} {msg}");
        }

        public void Error( string msg )
        {
            _logger.LogError($"{_loggerPrefixStack.Peek()} {msg}");
        }

        public void Warning( string msg )
        {
            _logger.LogWarning($"{_loggerPrefixStack.Peek()} {msg}");
        }

    }
}