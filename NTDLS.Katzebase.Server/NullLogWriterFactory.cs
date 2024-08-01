using Topshelf.Logging;

namespace NTDLS.Katzebase.Server
{
    /// <summary>
    /// A huge class to keep topshelf from polluting the console.
    /// </summary>
    public class NullLogWriterFactory : HostLoggerConfigurator
    {
        public LogWriterFactory CreateLogWriterFactory()
        {
            return new NullLogWriterFactoryImpl();
        }

        private class NullLogWriterFactoryImpl : LogWriterFactory
        {
            public LogWriter Get(string name)
            {
                return new NullLogWriter();
            }

            public void Shutdown() { }
        }

        private class NullLogWriter : LogWriter
        {
            public bool IsDebugEnabled => false;
            public bool IsInfoEnabled => false;
            public bool IsWarnEnabled => false;
            public bool IsErrorEnabled => false;
            public bool IsFatalEnabled => false;

            public void Debug(LogWriterOutputProvider messageProvider) { }
            public void Debug(object obj) { }
            public void Debug(object obj, Exception exception) { }
            public void DebugFormat(IFormatProvider formatProvider, string format, params object[] args) { }
            public void DebugFormat(string format, params object[] args) { }
            public void Error(LogWriterOutputProvider messageProvider) { }
            public void Error(object obj) { }
            public void Error(object obj, Exception exception) { }
            public void ErrorFormat(IFormatProvider formatProvider, string format, params object[] args) { }
            public void ErrorFormat(string format, params object[] args) { }
            public void Fatal(LogWriterOutputProvider messageProvider) { }
            public void Fatal(object obj) { }
            public void Fatal(object obj, Exception exception) { }
            public void FatalFormat(IFormatProvider formatProvider, string format, params object[] args) { }
            public void FatalFormat(string format, params object[] args) { }
            public void Info(LogWriterOutputProvider messageProvider) { }
            public void Info(object obj) { }
            public void Info(object obj, Exception exception) { }
            public void InfoFormat(IFormatProvider formatProvider, string format, params object[] args) { }
            public void InfoFormat(string format, params object[] args) { }
            public void Log(LoggingLevel level, LogWriterOutputProvider messageProvider) { }
            public void Log(LoggingLevel level, object obj) { }
            public void Log(LoggingLevel level, object obj, Exception exception) { }
            public void LogFormat(LoggingLevel level, IFormatProvider formatProvider, string format, params object[] args) { }
            public void LogFormat(LoggingLevel level, string format, params object[] args) { }
            public void Warn(LogWriterOutputProvider messageProvider) { }
            public void Warn(object obj) { }
            public void Warn(object obj, Exception exception) { }
            public void WarnFormat(IFormatProvider formatProvider, string format, params object[] args) { }
            public void WarnFormat(string format, params object[] args) { }

        }
    }
}
