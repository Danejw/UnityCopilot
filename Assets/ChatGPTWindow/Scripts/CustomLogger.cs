using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.Rendering;

namespace UnityCopilot.Log
{
    public class CustomLogger
    {
        public readonly Queue<string> errorLog = new Queue<string>();
        public readonly Queue<string> exceptionLog = new Queue<string>();
        public readonly Queue<string> messageLog = new Queue<string>();
        public readonly Queue<string> warningLog = new Queue<string>();

        public void LogFormat(LogType logType, string message)
        {
            switch (logType)
            {
                case LogType.Error:
                    errorLog.Enqueue(message);

                    // keep errorLog queue from growing indefinitely
                    while (errorLog.Count > 100)
                        errorLog.Dequeue();

                    break;
                case LogType.Exception:
                    exceptionLog.Enqueue(message);

                    // keep exceptionLog queue from growing indefinitely
                    while (exceptionLog.Count > 100)
                        exceptionLog.Dequeue();

                    break;
                case LogType.Warning:
                    warningLog.Enqueue(message);

                    // keep warningLog queue from growing indefinitely
                    while (warningLog.Count > 100)
                        warningLog.Dequeue();

                    break;
                case LogType.Log:
                    messageLog.Enqueue(message);

                    // keep messageLog queue from growing indefinitely
                    while (messageLog.Count > 100)
                        messageLog.Dequeue();

                    break;
            }
        }


        // Get the latest logs
        public string GetLatestError()
        {
            if (errorLog.Count > 0)
            {
                return errorLog.Peek();
            }
            return null;
        }

        public string GetLatestExceptions()
        {
            if (exceptionLog.Count > 0)
            {
                return exceptionLog.Peek();
            }
            return null;
        }

        public string GetLatestWarning()
        {
            if (warningLog.Count > 0)
            {
                return warningLog.Peek();
            }
            return null;
        }

        public string GetLatestMessage()
        {
            if (messageLog.Count > 0)
            {
                return messageLog.Peek();
            }
            return null;
        }
    }
}
