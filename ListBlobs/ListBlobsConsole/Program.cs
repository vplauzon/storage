using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
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
            var targetUrl = Environment.GetEnvironmentVariable("TARGET_URL");

            Console.WriteLine($"BLOB_DIR:  {blobDir}");
            Console.WriteLine($"SAS_TOKEN:  {sasToken}");
            Console.WriteLine($"TARGET_URL:  {targetUrl}");
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

                MainAsync(containerUri, folderPath, sasToken, targetUrl).Wait();
            }
        }

        private static async Task MainAsync(
            Uri containerUri,
            string folderPath,
            string sasToken,
            string targetUrl)
        {
            using (var stream = await GetTargetStreamAsync(targetUrl))
            {
                var writer = stream == null
                    ? Console.Out
                    : new StreamWriter(stream);
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
                        writer.WriteLine(i.Uri);
                    }
                    continuationToken = segment.ContinuationToken;
                }
                while (continuationToken != null);
                await writer.FlushAsync();
            }
        }

        private static async Task<Stream> GetTargetStreamAsync(string targetUrl)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return null;
            }
            else
            {
                var blob = new CloudBlockBlob(new Uri(targetUrl));
                var stream = await blob.OpenWriteAsync();

                return stream;
            }
        }
    }
}