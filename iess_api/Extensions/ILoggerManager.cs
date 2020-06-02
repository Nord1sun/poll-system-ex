using iess_api.Models;

namespace iess_api.Extensions
{
    public interface ILoggerManager
    {
        void LogInfo(string message, ExceptionModel exceptionModel);
        void LogWarn(string message, ExceptionModel exceptionModel);
        void LogDebug(string message, ExceptionModel exceptionModel);
        void LogError(string message, ExceptionModel exceptionModel);
    }
}