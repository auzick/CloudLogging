# CloudLogging
Helper library for using Azure Storage for application logging.

*To use this library, you can use this nuget package: https://www.nuget.org/packages/AndyUzick.AzureAppLogging.*

When building Azure Web Apps, I needed an easy way to log application messages persistently. This library is what I came up with. 

The idea is that you use an Azure Storage Blob Container to store a series of AppendBlobs. 
This library is all the helper methods to simplify this in your applications.

Of course, the ultimate goal is to monitor these logs. For that, I built a "tail" application for monitoring these "log blobs".
See https://github.com/auzick/BlobTail.

The LogManager class manages access to your logs. For example:

    var connectionString = ConfigurationManager.ConnectionStrings["LogStorageAccount"].ConnectionString
    
    var ILog myRawLog = (RawLog) LogManager.GetLog<RawLog>(connectionString, "mycontainername1");  
    myRawLog.Write("Some text for the log");

    var ILog myAppLog = (ApplicationLog) LogManager.GetLog<ApplicationLog>(connectionString, "mycontainername2");  
    myAppLog.Info("Here is an informational message");
    
There are two kinds of logs supplied.
- RawLog, which simply writes the supplied text to the blob.
- ApplicationLog, which writes prettily-formatted messages, with severity and date/time. There are static methods for writing messages of different severity (Debug, Info, Warn, Error, Fatal). 

The library will automatically create the container if it does not exist. 
It will also create the blob files using the pattern {DateTime.Now:yyyyMMdd}{sequence}.log, 
where sequence is incremented for every log file created that day.
The library will roll over to a new log file when the size limits are hit.
The library writes log messages to these blobs, up to the set limits (49k messages or 10mb by default).

For convenience, I create a class at the root of my web apps like this:

    using System;
    using CloudLogging;
    using Webhooks.Configuration;

    namespace Webhooks
    {
        public static class Log
        {
            private static readonly ApplicationLog InnerLog;

            static Log()
            {
                InnerLog = (ApplicationLog)LogManager.GetLog<ApplicationLog>(
                    Settings.LogStorageAccount, 
                    Settings.LogContainerName
                    );
                InnerLog.IsBuffering = true;
                InnerLog.MaxBufferAge = TimeSpan.FromSeconds(3);
                InnerLog.MaxBufferLength = 100;
                InnerLog.MinLogLevel = ApplicationLog.Level.Debug;
            }

            public static void Debug(string message)
            {
                InnerLog.Debug(message);
            }

            public static void Info(string message)
            {
                InnerLog.Info(message);
            }

            public static void Warn(string message)
            {
                InnerLog.Warn(message);
            }

            public static void Error(string message)
            {
                InnerLog.Error(message);
            }

            public static void Fatal(string message)
            {
                InnerLog.Fatal(message);
            }

            public static void Flush()
            {
                InnerLog.FlushBuffer();
            }

        }
    }

This lets me write log messages from anywhere in the app simply:

    Log.Info($"Something happened {someVariable}");
    

