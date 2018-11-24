namespace Airswipe.WinRT.Core.Log
{
    public interface ILogger
    {
        void Critical(string message, params object[] args);

        void Info(string message, params object[] args);

        void Warn(string message, params object[] args);

        void Error(string message, params object[] args);

        void All(string message, params object[] args);

        void Verbose(string message, params object[] args);

    }
}
