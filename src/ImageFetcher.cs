using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.Storage.V1;

namespace RugTextureGenerator
{
    public struct FetcherConfig 
    {
        public string APIUrl { get; set; }

        public string ImageSourceURL { get; set; }

        public string ImageServerName { get; set; }
        
        public List<string> Processed { get; set; }

        public bool IsNull() 
        {
            return (String.IsNullOrEmpty(this.APIUrl) && (this.Processed == null || this.Processed.Count == 0) && String.IsNullOrEmpty(this.ImageServerName));
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

        public void Save(string path)
        {
            FetcherConfig config = this;
            
            using (StreamWriter writer = new StreamWriter(path))
                writer.Write(JsonSerializer.Serialize(config));
        }

        public FetcherConfig(string apiURL, string imageSourceUrl, string[] processed)
        {
            this.APIUrl = apiURL;
            this.ImageSourceURL = imageSourceUrl;
            this.Processed = new List<string>();

            for (int x = 0; x < processed.Length; x++)
                this.Processed.Add(processed[x]);
        }

    }

    public class ImageFetcher
    {
        public string ConfigPath;
 
        public static string ImagePath = "Images";

        public StorageClient ImageStorageClient;
        
        public FetcherConfig Config;

        protected HttpClient WebClient;

        protected WebClient Client;

        public Range IDRange;

        
        public void Fetch()
        {
            Hashtable fetchedHash = null;

            Image image = null;
            
            for (int x = this.IDRange.Start.Value; x < this.IDRange.End.Value; x++)
            {
                string fetchedStr;
                
                Console.WriteLine($"{this.Config.APIUrl}/{x}");

                try
                {
                    if ((fetchedStr = JsonSerializer.Deserialize<Hashtable>(this.WebClient.GetStreamAsync($"{this.Config.APIUrl}/{x}").Result)["data"].ToString()) == null)
                        continue;
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Null request response.");

                    continue;
                }

                fetchedHash = JsonSerializer.Deserialize<Hashtable>(fetchedStr);
              
                Console.WriteLine($"{fetchedHash["productImages"].ToString()}");

                JsonElement images;

                string fetchedImage = null, imageName = null;
                
                if ((images = (JsonElement)fetchedHash["productImages"]).EnumerateArray().Count() == 0)
                {
                    Console.WriteLine("Null images.");
  
                    continue;
                }
                //
                // JsonElement.ArrayEnumerator enumerator; 
                //
                // enumerator = images.EnumerateArray();
                //
                // enumerator.MoveNext();
                //
                // fetchedImage= enumerator.Current.ToString(); 
               
                Console.WriteLine($"Retreived image path: {fetchedImage}");

                string[] splitArray = images[0].ToString().Split("/");

                imageName = splitArray[splitArray.Length - 1];
                
                Console.WriteLine($"Processed Images: {this.Config.Processed.Count}");

                if (Tools.LinearSearch(imageName, this.Config.Processed) != -1)
                {
                    Console.WriteLine("Exists, continuing");
                    continue;
                }
                
                using (WebClient wc = new WebClient())
                {
                    Console.WriteLine($"Downloading {images[0]}");
                    wc.DownloadFile($"{this.Config.ImageSourceURL}/{imageName}", $"{ImageFetcher.ImagePath}/Dump/{imageName}");
                }
                
                image = ImageTools.LoadImage($"{ImageFetcher.ImagePath}/Dump/{imageName}");

                ImageTools.Crop(image, 185, 10, 420, 650).Save($"{ImageFetcher.ImagePath}/Textures/{fetchedHash["designName"].ToString()}.jpg");
                
                this.Config.Processed.Add(fetchedImage); 
            }
        }

        public void Save()
        {
            this.Config.Save(this.ConfigPath); 
        }

        protected string[] GetServerImageDump()
        {
            var objects = this.ImageStorageClient.ListObjects(this.Config.ImageServerName).ToArray();

            string[] imageDump = new string[objects.Length], split;

            for (int x = 0; x < objects.Length; x++)
                imageDump[x] = (split = objects[x].Name.Split('/'))[split.Length - 1];

            Array.Sort(imageDump);

            return imageDump;
        } 

        public void UploadImages()
        {
            string[] dirList = Directory.GetFiles($"{ImageFetcher.ImagePath}/Textures"), imageDump = this.GetServerImageDump(), split;
            
            Console.WriteLine(JsonSerializer.Serialize(imageDump));
            
            for (int x = 0; x < dirList.Length; x++)
                if ((split = dirList[x].Split('.'))[split.Length - 1] == "png" || split[split.Length - 1] == "jpg" && Tools.LinearSearch<string>((split = dirList[x].Split('/'))[split.Length - 1], imageDump) == -1)
                    using (FileStream file = File.OpenRead($"{dirList[x]}"))
                    {
                        Console.WriteLine($"Uploading {split[split.Length - 1]}");
                        this.ImageStorageClient.UploadObject(this.Config.ImageServerName,
                            $"textures/{split[split.Length - 1]}",
                            "image/jpeg", file);
                    }
        }

        public ImageFetcher(string path, Range range)
        {
            this.Config = FetcherConfig.LoadFromFile(path);
            
            this.ConfigPath = path;
            this.IDRange = range;

            this.WebClient = new HttpClient();
            
            this.Client = new WebClient();

            if (!Directory.Exists(ImageFetcher.ImagePath))
            {
                Directory.CreateDirectory(ImageFetcher.ImagePath);
                
                Directory.CreateDirectory($"{ImageFetcher.ImagePath}/Dump");
                Directory.CreateDirectory($"{ImageFetcher.ImagePath}/Textures");
            }
            this.ImageStorageClient = StorageClient.Create();
            
        }
    }
} 
