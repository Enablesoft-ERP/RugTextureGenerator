namespace RugTextureGenerator;

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
}