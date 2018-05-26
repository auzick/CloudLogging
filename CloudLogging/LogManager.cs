using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;

namespace CloudLogging
{
    public static class LogManager
    {
        private static readonly object LogsLock = new object();

        public static IDictionary<string, ILog> Logs;

        static LogManager()
        {
            Logs = new Dictionary<string, ILog>();
        }

        public static ILog GetLog<T>(CloudStorageAccount account, string containerName) where T:ILog
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name must be specified.");
            lock (LogsLock)
            {
                if (Logs.ContainsKey(containerName))
                {
                    var existing = Logs[containerName];
                    if (! existing.GetType().IsAssignableFrom(typeof(T)) )
                        throw new TypeAccessException($"The existing log cannot be cast to {typeof(T).FullName}");
                    return existing;
                }
                var log = (ILog)Activator.CreateInstance(typeof(T), account, containerName);
                Logs.Add(containerName, log);
                return (T)Logs[containerName];
            }
        }

    }
}
