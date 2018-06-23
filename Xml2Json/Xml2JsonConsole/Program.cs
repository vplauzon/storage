using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace Xml2JsonConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = new Uri(Environment.GetEnvironmentVariable("DIR"));
            var sasToken = Environment.GetEnvironmentVariable("SAS-TOKEN");

            MainAsync(dir, sasToken).Wait();
        }

        private static async Task MainAsync(Uri dir, string sasToken)
        {
            var storageCreds = new StorageCredentials(sasToken);
            var container = new CloudBlobContainer(dir, storageCreds);
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