using Katzebase.Engine.Exceptions;
using System;
using System.IO;
using System.Text;

namespace Katzebase.Engine.Logging
{
    public class LogManager
    {
        private Core core;
        private System.IO.StreamWriter fileHandle = null;
        private DateTime recycledTime = DateTime.MinValue;

        public LogManager(Core core)
        {
            this.core = core;
            RecycleLog();
        }

        public void Start()
        {
            RecycleLog();
        }

        public void Stop()
        {
            Close();
        }

        public void Checkpoint()
        {
            lock (this)
            {
                if (fileHandle != null)
                {
                    try
                    {
                        fileHandle.Flush();
                    }
                    catch
                    {
                        //Discard
                    }
                }
            }
        }

        public void Write(LogEntry entry)
        {
            if (entry.Severity == Constants.LogSeverity.Trace && core.settings.WriteTraceData == false)
            {
                return;
            }

            if (entry.Exception != null)
            {
                if (typeof(KatzebaseExceptionBase).IsAssignableFrom(entry.Exception.GetType()))
                {
                    entry.Severity = ((KatzebaseExceptionBase)entry.Exception).Severity;
                }
            }

            lock (this)
            {
                if (entry.Severity == Constants.LogSeverity.Warning)
                {
                    core.Health.Increment(Constants.HealthCounterType.Warnings);
                }
                else if (entry.Severity == Constants.LogSeverity.Exception)
                {
                    core.Health.Increment(Constants.HealthCounterType.Exceptions);
                }

                RecycleLog();

                StringBuilder message = new StringBuilder();

                message.AppendFormat("{0}|{1}|{2}", entry.DateTime.ToShortDateString(), entry.DateTime.ToShortTimeString(), entry.Severity);

                if (entry.Message != null && entry.Message != string.Empty)
                {
                    message.Append("|");
                    message.Append(entry.Message);
                }

                if (entry.Exception != null)
                {
                    if (typeof(KatzebaseExceptionBase).IsAssignableFrom(entry.Exception.GetType()))
                    {
                        if (entry.Exception.Message != null && entry.Exception.Message != string.Empty)
                        {
                            message.AppendFormat("|Exception: {0}: ", entry.Exception.GetType().Name);
                            message.Append(entry.Exception.Message);
                        }
                    }
                    else
                    {
                        if (entry.Exception.Message != null && entry.Exception.Message != string.Empty)
                        {
                            message.Append("|Exception: ");
                            message.Append(GetExceptionText(entry.Exception));
                        }

                        if (entry.Exception.StackTrace != null && entry.Exception.StackTrace != string.Empty)
                        {
                            message.Append("|Stack: ");
                            message.Append(entry.Exception.StackTrace);
                        }
                    }
                }

                if (entry.Severity == Constants.LogSeverity.Warning)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                }
                else if (entry.Severity == Constants.LogSeverity.Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (entry.Severity == Constants.LogSeverity.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.WriteLine(message.ToString());

                Console.ForegroundColor = ConsoleColor.Gray;

                fileHandle.WriteLine(message.ToString());

                if (core.settings.FlushLog)
                {
                    fileHandle.Flush();
                }
            }
        }

        private string GetExceptionText(Exception ex)
        {
            StringBuilder message = new StringBuilder();
            return GetExceptionText(ex, 0, ref message);
        }

        private string GetExceptionText(Exception ex, int level, ref StringBuilder message)
        {
            if (ex.Message != null && ex.Message != string.Empty)
            {
                message.AppendFormat("{0} {1}", level, ex.Message);
            }

            if (ex.InnerException != null && level < 100)
            {
                return GetExceptionText(ex.InnerException, level + 1, ref message);
            }

            return message.ToString();
        }


        public void Write(string message)
        {
            Write(new LogEntry(message) { Severity = Constants.LogSeverity.Verbose });
        }

        public void Trace(string message)
        {
            Write(new LogEntry(message) { Severity = Constants.LogSeverity.Trace });
        }

        public void Write(string message, Exception ex)
        {
            Write(new LogEntry(message) { Exception = ex, Severity = Constants.LogSeverity.Exception });
        }

        public void Write(string message, Constants.LogSeverity severity)
        {
            Write(new LogEntry(message) { Severity = severity });
        }

        private void RecycleLog()
        {
            lock (this)
            {
                if (recycledTime.Date != DateTime.Now)
                {
                    Close();

                    recycledTime = DateTime.Now;
                    string fileName = core.settings.LogDirectory + "\\" + $"{recycledTime.Year}_{recycledTime.Month}_{recycledTime.Day}.txt";
                    Directory.CreateDirectory(core.settings.LogDirectory);
                    fileHandle = new StreamWriter(fileName, true);
                }
            }
        }

        public void Close()
        {
            if (fileHandle != null)
            {
                try
                {
                    fileHandle.Close();
                    fileHandle.Dispose();
                }
                catch
                {
                    //Discard
                }
            }
        }

    }
}
