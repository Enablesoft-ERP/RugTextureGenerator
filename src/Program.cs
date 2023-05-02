using System.Net;
using Google.Cloud.Storage.V1;

namespace RugTextureGenerator
{
    public class Program
    {
        public static void CreateTextures()
        {
            ImageFetcher fetcher = new ImageFetcher("FetcherConfig.json", new Range(35, 2000));
            
            // fetcher.Fetch();
            // fetcher.Save();

            fetcher.UploadImages();
        }

        public static void Main(string[] args)
        {
            Program.CreateTextures();
            // using (WebClient client = new WebClient())
            //     client.DownloadFile("https://test.mirzapurkaleenandrugs.com/product-1-90-638159731140425269-img.jpg", "image.jpg");
        } 
    }
}