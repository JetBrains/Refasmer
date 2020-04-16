using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Action<string> Trace => 
            _logger.IsEnabled(LogLevel.Trace) 
                ? msg => _logger.LogTrace($"{_loggerPrefixStack.Peek()} {msg}")
                : (Action<string>)null;
        public Action<string> Debug => 
            _logger.IsEnabled(LogLevel.Debug) 
                ? msg => _logger.LogDebug($"{_loggerPrefixStack.Peek()} {msg}")
                : (Action<string>)null;
        public Action<string> Info => 
            _logger.IsEnabled(LogLevel.Information) 
                ? msg => _logger.LogInformation($"{_loggerPrefixStack.Peek()} {msg}")
                : (Action<string>)null;
        public Action<string> Warning => 
            _logger.IsEnabled(LogLevel.Warning) 
                ? msg => _logger.LogWarning($"{_loggerPrefixStack.Peek()} {msg}")
                : (Action<string>)null;
        public Action<string> Error => 
            _logger.IsEnabled(LogLevel.Error) 
                ? msg => _logger.LogError($"{_loggerPrefixStack.Peek()} {msg}")
                : (Action<string>)null;
        public Action<string> Critical => 
            _logger.IsEnabled(LogLevel.Critical) 
                ? msg => _logger.LogCritical($"{_loggerPrefixStack.Peek()} {msg}")
                : (Action<string>)null;
    }
}