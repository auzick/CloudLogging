using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudLogging
{
    public static class StorageHelper
    {
        public static CloudBlobContainer GetBlobContainer(CloudStorageAccount account, string name)
        {
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(name.ToLower());
            if (!container.Exists())
            {
                container.CreateIfNotExists();
                container.SetPermissions(
                    new BlobContainerPermissions {PublicAccess = BlobContainerPublicAccessType.Blob});
            }
            return container;
        }

        public static CloudAppendBlob GetAppendBlob(CloudBlobContainer container, string blobName)
        {
            var blob = container.GetAppendBlobReference(blobName.ToLower());
            if (!blob.Exists())
            {
                blob.CreateOrReplace();
            }
            return blob;
        }


    }
}