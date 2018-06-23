using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace ListBlobsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var blobDir = new Uri(Environment.GetEnvironmentVariable("BLOB-DIR"));
            var sasToken = Environment.GetEnvironmentVariable("SAS-TOKEN");

            MainAsync(blobDir, sasToken).Wait();
        }

        private static async Task MainAsync(Uri blobDir, string sasToken)
        {
            var storageCreds = new StorageCredentials(sasToken);
            var container = new CloudBlobContainer(blobDir, storageCreds);
            BlobContinuationToken continuationToken = null;

            do
            {
                var segment = await container.ListBlobsSegmentedAsync(
                    "Partitioned_xmlFiles/",
                    true,
                    BlobListingDetails.Metadata,
                    null,
                    continuationToken,
                    null,
                    null);

                foreach (var i in segment.Results)
                {
                    Console.WriteLine(i.Uri);
                }
                continuationToken = segment.ContinuationToken;
            }
            while (continuationToken != null);
        }
    }
}