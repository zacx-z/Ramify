using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nela.Ramify {
    public class Logger {
        public static List<ILogger> loggers = new List<ILogger>();
        public static void LogError(Exception exception, View view, UnityEngine.Object context = null) {
            Debug.LogError($"Injection error: (Check the error by Inspecting Bindings button on the inspector of the target object)\nContext:{context}\n{exception.Message}\n{exception.StackTrace}", view);

            foreach (var logger in loggers) {
                logger.Log(exception, view, context);
            }
        }
    }

    public interface ILogger {
        void Log(Exception exception, View view, Object context);
    }
}