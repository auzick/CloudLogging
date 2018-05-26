using System;
using System.IO;
using System.Text;
using CloudLogging.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudLogging
{
    public abstract class Log
    {
        private readonly CloudBlobContainer _container;
        private int _currentLogSequence;

        protected Log(CloudStorageAccount account, string logContainerName)
        {
            _container = StorageHelper.GetBlobContainer(account, logContainerName);
        }

        private CloudAppendBlob _currentBlob;
        protected CloudAppendBlob CurrentBlob
        {
            get
            {
                if (_currentBlob == null || _currentBlob.Name != GetLogName())
                    _currentBlob = StorageHelper.GetAppendBlob(_container, GetLogName());

                while (
                    _currentBlob.Properties.AppendBlobCommittedBlockCount > Settings.MaxLogBlocks
                    && _currentBlob.Properties.Length > Settings.MaxLogSize
                )
                {
                    _currentLogSequence++;
                    _currentBlob = StorageHelper.GetAppendBlob(_container, GetLogName());
                }

                return _currentBlob;
            }
        }

        protected string GetLogName()
        {
            var sequence = _currentLogSequence > 0 ? $"-{_currentLogSequence}" : "";
            var name = $"{DateTime.Now:yyyyMMdd}{sequence}.log";
            return name;
        }

        public void Write(string message)
        {
            // Changing this from AppendText to AppendBlob to better manage concurrency
            // https://stackoverflow.com/questions/32530126/azure-cloudappendblob-errors-with-concurrent-access
            // CurrentBlob.AppendText($"{message}{Environment.NewLine}");

            var msg = $"{message}{(message.EndsWith(Environment.NewLine) ? string.Empty : Environment.NewLine)}";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes($"{msg}")))
            {
                CurrentBlob.AppendBlock(ms);
            }
        }
    }
}