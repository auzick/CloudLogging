using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Microsoft.WindowsAzure.Storage;

namespace CloudLogging
{
    public class ApplicationLog : Log, ILog
    {
        public enum Level
        {
            Debug,
            Info,
            Warn,
            Error,
            Fatal
        }

        private static readonly Level[] Levels =
            Enum.GetValues(typeof(Level))
                .Cast<Level>()
                .ToArray();

        public Level MinLogLevel { get; set; }

        private Level[] _levels;

        private Level[] AllowedLevels =>
            _levels ?? (_levels = Levels
                .Skip(Array.IndexOf(Levels, MinLogLevel))
                .Take(Levels.Length - Array.IndexOf(Levels, MinLogLevel) + 1)
                .ToArray());

        public int MaxBufferLength { get; set; }
        public TimeSpan MaxBufferAge { get; set; }

        private static readonly object BufferLock = new object();

        protected readonly Queue<string> Buffer = new Queue<string>();

        private bool _isBuffering;
        public bool IsBuffering
        {
            get { return _isBuffering; }
            set
            {
                _isBuffering = value;
                if (value == false)
                    FlushBuffer();
            }
        }

        public ApplicationLog(CloudStorageAccount account, string logContainerName) : base(account, logContainerName)
        {
            MaxBufferLength = 100;
            MaxBufferAge = TimeSpan.FromSeconds(3);
            _isBuffering = true;
            var timer = new Timer();
            timer.Elapsed += BufferTimeElapsed;
            timer.Interval = MaxBufferAge.TotalMilliseconds;
            timer.Enabled = true;
            MinLogLevel = Level.Info;
        }

        protected void WriteLog(Level level, string message)
        {
            if (!AllowedLevels.Any(l => l == level))
                return;

            if (IsBuffering)
                WriteBuffer($"{level.ToString().ToUpper()} {DateTime.UtcNow:u}: {message}");
            else
                Write($"{level.ToString().ToUpper()} {DateTime.UtcNow:u}: {message}");
        }


        // ------------------------------------------------------------------------
        // Convenience methods

        public void Debug(string message)
        {
            WriteLog(Level.Debug, message);
        }

        public void Info(string message)
        {
            WriteLog(Level.Info, message);
        }

        public void Warn(string message)
        {
            WriteLog(Level.Warn, message);
        }

        public void Error(string message)
        {
            WriteLog(Level.Error, message);
        }

        public void Fatal(string message)
        {
            WriteLog(Level.Fatal, message);
        }

        // ------------------------------------------------------------------------
        // Buffering methods 

        private void WriteBuffer(string text)
        {
            lock (BufferLock)
            {
                Buffer.Enqueue(text);
            }
            if (Buffer.Count >= MaxBufferLength)
            {
                FlushBuffer();
            }
        }

        public void FlushBuffer()
        {
            lock (BufferLock)
            {
                while (Buffer.Count > 0)
                {
                    Write(Buffer.Dequeue());
                }
            }
        }

        private void BufferTimeElapsed(object sender, ElapsedEventArgs e)
        {
            if (Buffer.Count > 0)
                FlushBuffer();
        }
    }
}
