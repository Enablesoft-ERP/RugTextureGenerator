using System.Data.SqlTypes;
using System.Diagnostics.Contracts;

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text.Json;

namespace RugTextureGenerator
{
    public struct FetcherConfig 
    {
        public string APIUrl { get; set; }

        public List<string> Processed { get; set; }

        public bool IsNull() 
        {
            return ((this.APIUrl == null || this.APIUrl.Length == 0) && (this.Processed == null || this.Processed.Count == 0));
        }
        
        public static FetcherConfig LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"{path} does not exist, creating a new config file.");
                StreamWriter writer = new StreamWriter(path);
                
                writer.Write(JsonSerializer.Serialize(new FetcherConfig()));
                writer.Flush();
                return new FetcherConfig();
            }

            return JsonSerializer.Deserialize<FetcherConfig>(new StreamReader(path).ReadToEnd());
        }

        public FetcherConfig(string apiURL, string[] processed)
        {
            this.APIUrl = apiURL;
            this.Processed = new List<string>();

            for (int x = 0; x < processed.Length; x++)
                this.Processed.Add(processed[x]);
        }

    }

    public class ImageFetcher
    {
        public FetcherConfig Config;
        
        public Task Fetch()
        {
            return Task.Run(() => { });
        }

        public ImageFetcher(string path)
        {
            this.Config = FetcherConfig.LoadFromFile(path);
        }
    }
} 
