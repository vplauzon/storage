using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ListBlobsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var blobDir = Environment.GetEnvironmentVariable("BLOB_DIR");
            var sasToken = Environment.GetEnvironmentVariable("SAS_TOKEN");

            Console.WriteLine($"BLOB_DIR:  {blobDir}");
            Console.WriteLine($"SAS_TOKEN:  {sasToken}");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(blobDir))
            {
                Console.WriteLine("Missing BLOB_DIR environment variable");
            }
            else if (blobDir.Split('/').Length < 4)
            {
                Console.WriteLine("BLOB_DIR doesn't point to at least a container");
            }
            else if (string.IsNullOrWhiteSpace(sasToken))
            {
                Console.WriteLine("Missing SAS_TOKEN environment variable");
            }
            else
            {
                var split = blobDir.Split('/');
                var containerUri = new Uri(string.Join('/', split.Take(4)));
                var folderPath = string.Join('/', split.Skip(4));

                MainAsync(containerUri, folderPath, sasToken).Wait();
            }
        }

        private static async Task MainAsync(Uri containerUri, string folderPath, string sasToken)
        {
            var storageCreds = new StorageCredentials(sasToken);
            var container = new CloudBlobContainer(containerUri, storageCreds);
            var folder = container.GetDirectoryReference(folderPath);
            BlobContinuationToken continuationToken = null;

            do
            {
                var segment = await folder.ListBlobsSegmentedAsync(
                    true,
                    BlobListingDetails.None,
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