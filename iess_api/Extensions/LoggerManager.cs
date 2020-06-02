using iess_api.Interfaces;
using iess_api.Models;
using NLog;
using ILogger = NLog.ILogger;

namespace iess_api.Extensions
{
    public class LoggerManager : ILoggerManager
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IExceptionRepository _exceptionRepository;

        public LoggerManager(IExceptionRepository exceptionRepository)
        {
            _exceptionRepository = exceptionRepository;
        }

        public void LogDebug(string message, ExceptionModel exceptionModel)
        {
            logger.Debug(message);
            _exceptionRepository.PostAsync(exceptionModel);
        }

        public void LogError(string message, ExceptionModel exceptionModel)
        {
            logger.Error(message);
            _exceptionRepository.PostAsync(exceptionModel);
        }

        public void LogInfo(string message, ExceptionModel exceptionModel)
        {
            logger.Info(message);
            _exceptionRepository.PostAsync(exceptionModel);
        }

        public void LogWarn(string message, ExceptionModel exceptionModel)
        {
            logger.Warn(message);
            _exceptionRepository.PostAsync(exceptionModel);
        }
    }
}
