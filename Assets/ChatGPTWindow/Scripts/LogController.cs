using UnityEngine;

namespace UnityCopilot.Log
{
    public partial class LogController
    {
        private readonly LogQueue errorLog = new();
        private readonly LogQueue exceptionLog = new();
        private readonly LogQueue messageLog = new();
        private readonly LogQueue warningLog = new();

        public void LogFormat(LogType logType, string message)
        {
            switch (logType)
            {
                case LogType.Error:
                    errorLog.Add(message);
                    errorLog.TrimLogQueue();
                    break;
                case LogType.Exception:
                    exceptionLog.Add(message);
                    exceptionLog.TrimLogQueue();
                    break;
                case LogType.Warning:
                    warningLog.Add(message);
                    warningLog.TrimLogQueue();
                    break;
                case LogType.Log:
                    messageLog.Add(message);
                    messageLog.TrimLogQueue();
                    break;
            }
        }

        public LogQueue GetErrorLog() => errorLog;

        public LogQueue GetExceptionLog() => exceptionLog;

        public LogQueue GetMessageLog() => messageLog;

        public LogQueue GetWarningLog() => warningLog;
    }
}
