using SixLabors.ImageSharp;

namespace RugTextureGenerator
{
    public class ImageTools
    {
        public static Image LoadImage(string path)
        {
            Image image = null;
            
            if (File.Exists(path)) 
                using (FileStream stream = File.OpenRead(path))
                    image = Image.Load(stream);

            return image;
        }

        public static Image Crop(Image image, int x, int y, int width, int height)
        {
            image.Mutate(img => { img.Crop(Rectangle.FromLTRB(x, y, x + width, y + height)); });

            return image;
        }
    }
}
