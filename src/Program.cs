namespace RugTextureGenerator
{
    public class Program
    {
        public static void CreateTextures()
        {
        }

        public static void Main(string[] args)
        {
            Image image = ImageTools.LoadImage(args[0]);

            image = ImageTools.Crop(image, int.Parse(args[1]), int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]));

            image.Save(args[5]);
        } 
    }
}