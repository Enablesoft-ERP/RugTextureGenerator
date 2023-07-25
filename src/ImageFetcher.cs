using System.Collections;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Google.Cloud.Storage.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace RugTextureGenerator
{
    public struct FetcherConfig 
    {
        public string APIUrl { get; set; }

        public string ImageSourceURL { get; set; }

        public string ImageServerName { get; set; }
        
        public List<string> Processed { get; set; }

        public List<string> Failed { get; set; }

        public bool IsNull() 
        {
            return (String.IsNullOrEmpty(this.APIUrl) && (this.Processed == null || this.Processed.Count == 0) && (this.Failed == null || this.Failed.Count == 0) && String.IsNullOrEmpty(this.ImageServerName));
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

        public FetcherConfig(string apiURL, string imageSourceUrl, string[] processed, string[] failed = null)
        {
            this.APIUrl = apiURL;
            this.ImageSourceURL = imageSourceUrl;
            this.Processed = new List<string>();
            this.Failed = new List<string>();

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

        protected SHA256 ShaInstance;
        
        protected string CheckSum;

        protected Dictionary<string, string> ImageURLMap;

        protected Dictionary<string, bool> ProccesedMap;

        public void GetUrls()
        {
            List<string> urls = new List<string>(); 
           
            string fetchedStr;

            if ((fetchedStr = JsonSerializer.Deserialize<Hashtable>(this.WebClient.GetStreamAsync($"{this.Config.APIUrl}/")
                    .Result)["data"].ToString()) == null)
            {
                throw new Exception("Failed to fetch products.");
            }
            
            List<Hashtable> fetchedHash = System.Text.Json.JsonSerializer.Deserialize<List<Hashtable>>(fetchedStr);

            for (int x = 0; x < fetchedHash.Count; x++)
            {
                string url;
                string[] split = (url = fetchedHash[x]["primePhoto"].ToString()).Split("/");
            
                if (split[split.Length - 1] != "no-image.png")
                    if (!this.ImageURLMap.ContainsKey(url))
                        this.ImageURLMap.Add(url, fetchedHash[x]["designName"].ToString());
            } 
        }

        public void Fetch()
        {
            Hashtable fetchedHash = null;

            List<Hashtable> productList =  new List<Hashtable>();

            Image image = null;

            Dictionary<string, string>.KeyCollection keys = this.ImageURLMap.Keys;

            this.GetUrls();
            
            foreach (string imageURL in keys)
            {
                string fetchedImage = null, imageName = null, imagePath;

                string[] splitArray;
                
                if ((imageName = (splitArray = imageURL.Split("/"))[splitArray.Length - 1]) == "no-image.png")
                {
                    Console.WriteLine("Null images.");
                    continue;
                }
               
                Console.WriteLine($"Retreived image path: {fetchedImage}");

                
                Console.WriteLine($"Processed Images: {this.Config.Processed.Count}");

                imagePath = $"{ImageFetcher.ImagePath}/Dump/{imageName}";
                
                if (File.Exists(imagePath))
                {
                    Console.WriteLine("Exists, continuing");
                    this.AddProcessed(imageName);
                    continue;
                }
                
                using (WebClient wc = new WebClient())
                {
                    Console.WriteLine($"Downloading {imageURL}");
                    wc.DownloadFile(imageURL, imagePath);
                }
                
                Console.WriteLine($"Loading {imagePath}");

                image = ImageTools.LoadImage(imagePath);
               
                if (image == null)
                {
                    Console.WriteLine("Loaded image is null");
                    
                    continue;
                }

                try
                {
                    ImageTools.Crop(image, 185, 10, 420, 650).Save($"{ImageFetcher.ImagePath}/Textures/{this.ImageURLMap[imageURL].ToString()}.jpg");
                }
                catch (Exception e)
                {
                    if (this.Config.Failed.IndexOf(imageName) < 0)
                        this.Config.Failed.Add(imageName); 
                    
                    continue;
                }
                
                this.AddProcessed(imageName);
            }  
        }

        public void Save()
        {
            this.Config.Save(this.ConfigPath); 
        }

        protected void AddProcessed(string imageName)
        {
            this.Config.Processed.Add(imageName);
            this.ProccesedMap.Add(imageName, true);
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

        public void LoadProcessed()
        {
            for (int x = 0; x < this.Config.Processed.Count; x++)
                if (this.Config.Processed[x] != null)
                this.ProccesedMap.Add(this.Config.Processed[x], true);
        }

        public ImageFetcher(string path)
        {
            this.Config = FetcherConfig.LoadFromFile(path);
            
            this.ConfigPath = path;

            this.WebClient = new HttpClient();
            
            this.Client = new WebClient();

            if (!Directory.Exists(ImageFetcher.ImagePath))
            {
                Directory.CreateDirectory(ImageFetcher.ImagePath);

                Directory.CreateDirectory($"{ImageFetcher.ImagePath}/Dump");
                Directory.CreateDirectory($"{ImageFetcher.ImagePath}/Textures");
            }

            this.ImageURLMap = new Dictionary<string, string>();
            this.ProccesedMap = new Dictionary<string, bool>();
            
            this.ImageStorageClient = StorageClient.Create();
            this.ShaInstance = SHA256.Create();
            
            this.LoadProcessed();
        }
    }
} 
