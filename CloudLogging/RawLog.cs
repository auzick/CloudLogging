using Microsoft.WindowsAzure.Storage;

namespace CloudLogging
{
    public class RawLog : Log, ILog
    {
        public RawLog(CloudStorageAccount account, string logContainerName) : base(account, logContainerName)
        {
        }
    }
}
