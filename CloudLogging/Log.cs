using System;
using CloudLogging.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudLogging
{
    public abstract class Log
    {
        private readonly CloudBlobContainer _container;

        private int _currentLogSequence = 0;
        private CloudAppendBlob _currentBlob;

        protected CloudAppendBlob CurrentBlob
        {
            get
            {
                if (_currentBlob == null || _currentBlob.Name != GetLogName())
                {
                    _currentBlob = StorageHelper.GetAppendBlob(_container, GetLogName());
                }

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

        protected Log(CloudStorageAccount account, string logContainerName)
        {
            _container = StorageHelper.GetBlobContainer(account, logContainerName);
        }

        protected string GetLogName()
        {
            var sequence = _currentLogSequence > 0 ? $"-{ _currentLogSequence}" : "";
            var name = $"{DateTime.Now:yyyyMMdd}{sequence}.log";
            return name;
        }

        public void Write(string message)
        {
            CurrentBlob.AppendText($"{message}{Environment.NewLine}");
        }

    }
}