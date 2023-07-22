namespace RugTextureGenerator
{
    public class Tools
    {
        public static int LinearSearch<T>(T val, T[] array)
        {
            for (int x = 0; x < array.Length; x++)
                if (val.Equals(array[x]))
                    return x;
            return -1;
        }

        public static int LinearSearch<T>(T val, List<T> array)
        {
            for (int x = 0; x < array.Count; x++)
                if (val.Equals(array[x]))
                    return x;
            return -1;
        }

        public static string GetStringFromBytes(byte[] bytes)
        {
            string str = "";

            for (int x = 0; x < bytes.Length; x++)
                str += bytes[x].ToString("x2");

            return str;
        }
    }
}