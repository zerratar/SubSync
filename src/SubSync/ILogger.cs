namespace SubSync
{
    internal interface ILogger
    {
        void Write(string message);
        void WriteLine(string message);
        void Error(string errorMessage);
    }
}