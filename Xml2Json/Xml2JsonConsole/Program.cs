using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Xml2JsonConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //    "BLOB_LIST_URL": "https://acpavimport.blob.core.windows.net/xmlfiles/xml-file-list.txt?st=2018-06-23T20%3A00%3A32Z&se=2018-06-24T20%3A00%3A32Z&sp=r&sv=2017-07-29&sr=b&sig=5ZcnHmSc45sm%2F5DgDo%2FEXc%2FZOpJttw9x%2BRYBeS5Zym8%3D",
            //"SPLIT_COUNT": "3",
            //"SPLIT_INDEX": "1",
            //"SAS-TOKEN": "?st=2018-06-23T20%3A01%3A13Z&se=2018-06-24T20%3A01%3A13Z&sp=r&sv=2017-07-29&sr=c&sig=Z8jF%2FWlGfbRFumW6oNYTFroZnBrJFsTzH2e%2F0gBkZ%2Bk%3D",
            //"TARGET_CONTAINER": "https://acpavimport.blob.core.windows.net/json-files?st=2018-06-23T20%3A02%3A29Z&se=2018-06-24T20%3A02%3A29Z&sp=w&sv=2017-07-29&sr=c&sig=7dUU%2BABwtyuBaJXhnKUkVL%2B%2BVi%2FtCr%2BFc%2Bwx9deyhQk%3D",
            //"PARALLELISM": "20"

            var blobListUrl = Environment.GetEnvironmentVariable("BLOB_LIST_URL");
            var splitCount = Environment.GetEnvironmentVariable("SPLIT_COUNT");
            var splitIndex = Environment.GetEnvironmentVariable("SPLIT_INDEX");
            var sasToken = Environment.GetEnvironmentVariable("SAS_TOKEN");
            var targetContainer = Environment.GetEnvironmentVariable("TARGET_CONTAINER");
            var parallelism = Environment.GetEnvironmentVariable("PARALLELISM");

            Console.WriteLine($"BLOB_LIST_URL:  {blobListUrl}");
            Console.WriteLine($"SPLIT_COUNT:  {splitCount}");
            Console.WriteLine($"SPLIT_INDEX:  {splitIndex}");
            Console.WriteLine($"SAS_TOKEN:  {sasToken}");
            Console.WriteLine($"TARGET_CONTAINER:  {targetContainer}");
            Console.WriteLine($"PARALLELISM:  {parallelism}");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(blobListUrl))
            {
                Console.WriteLine("Missing BLOB_LIST_URL environment variable");
            }
            else if (string.IsNullOrWhiteSpace(splitCount))
            {
                Console.WriteLine("Missing SPLIT_COUNT environment variable");
            }
            else if (string.IsNullOrWhiteSpace(splitIndex))
            {
                Console.WriteLine("Missing SPLIT_INDEX environment variable");
            }
            else if (string.IsNullOrWhiteSpace(sasToken))
            {
                Console.WriteLine("Missing SAS_TOKEN environment variable");
            }
            else if (string.IsNullOrWhiteSpace(targetContainer))
            {
                Console.WriteLine("Missing TARGET_CONTAINER environment variable");
            }
            else if (string.IsNullOrWhiteSpace(parallelism))
            {
                Console.WriteLine("Missing PARALLELISM environment variable");
            }
            else
            {
                MainAsync(
                    new Uri(blobListUrl),
                    int.Parse(splitCount),
                    int.Parse(splitIndex),
                    sasToken,
                    new Uri(targetContainer),
                    int.Parse(parallelism)).Wait();
            }
        }

        private static async Task MainAsync(
            Uri blobListUri,
            int splitCount,
            int splitIndex,
            string sasToken,
            Uri targetContainerUri,
            int parallelism)
        {
            var blobPaths = await GetBlobListAsync(blobListUri, splitCount, splitIndex);
            var targetContainer = new CloudBlobContainer(targetContainerUri);
            var transformTasks = new List<Task<bool>>();
            var success = 0;
            var failure = 0;
            var total = blobPaths.Count();
            var progressClip = Math.Max(1, total / 100);

            while (blobPaths.Any() || transformTasks.Any())
            {   //  Consider adding a new task
                if (transformTasks.Count() < parallelism && blobPaths.Any())
                {
                    var path = blobPaths.Pop();
                    var sourceUri = new Uri(path + sasToken);
                    var targetName = string.Join('/', sourceUri.AbsolutePath.Split('/').Skip(2));
                    var targetBlob = targetContainer.GetBlockBlobReference(targetName);

                    transformTasks.Add(TransformAsync(sourceUri, targetBlob));
                }
                else
                {   //  Wait for tasks
                    var task = await Task.WhenAny(transformTasks);

                    transformTasks.Remove(task);
                    if (task.Result)
                    {
                        ++success;
                    }
                    else
                    {
                        ++failure;
                    }

                    if (((success + failure) % progressClip) == 0)
                    {
                        Console.WriteLine($"{success + failure} blobs processed over {total} ; {failure} failures");
                    }
                }
            }
        }

        private static async Task<bool> TransformAsync(Uri sourceUri, CloudBlockBlob targetBlob)
        {
            var sourceBlob = new CloudBlockBlob(sourceUri);
            var xmlText = await sourceBlob.DownloadTextAsync();
            var (jsonText, message) = GetJsonText(xmlText);

            if (jsonText != null)
            {
                await targetBlob.UploadTextAsync(jsonText);

                return true;
            }
            else
            {
                Console.WriteLine($"Malformed XML:  {sourceUri} ; {message}");

                return false;
            }
        }

        private static (string jsonText, string message) GetJsonText(string xmlText)
        {
            var doc = new XmlDocument();

            try
            {
                doc.LoadXml(xmlText);

                var jsonText = JsonConvert.SerializeXmlNode(doc);

                return (jsonText, null);
            }
            catch (XmlException ex)
            {
                return (null, ex.Message);
            }
        }

        private static async Task<Stack<string>> GetBlobListAsync(
            Uri blobListUri,
            int splitCount,
            int splitIndex)
        {
            var listBlob = new CloudBlockBlob(blobListUri);
            var text = await listBlob.DownloadTextAsync();
            var allLines = text.Split(Environment.NewLine);
            var firstLine = splitIndex * allLines.Length / splitCount;
            var afterLastLine = (splitIndex + 1) * allLines.Length / splitCount;
            var lines = allLines.Skip(firstLine).Take(afterLastLine - firstLine).ToArray();

            return new Stack<string>(lines.Reverse());
        }
    }
}