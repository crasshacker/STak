using System;
using NLog;

namespace STak.TakHub.Infrastructure.Logging
{
    public class Logger : Core.Interfaces.Services.ILogger
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();


        public void LogDebug(string message) => s_logger.Debug(message);
        public void LogError(string message) => s_logger.Error(message);
        public void LogInfo (string message) => s_logger.Info (message);
        public void LogWarn (string message) => s_logger.Warn (message);
    }
}
