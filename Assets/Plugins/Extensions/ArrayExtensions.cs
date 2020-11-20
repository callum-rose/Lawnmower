namespace BalsamicBits.Extensions
{
    public static class ArrayExtensions
    {
        public static T[] Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
                arr[i] = value;

            return arr;
        }

        public static T[,] Resize<T>(this T[,] arr, int newWidth, int newDepth, T defaultValue = default)
        {
            int oldWidth = arr.GetLength(0);
            int oldDepth = arr.GetLength(1);

            T[,] newArr = new T[newWidth, newDepth];

            for (int y = 0; y < newDepth; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    newArr[x, y] = x < oldWidth && y < oldDepth ? arr[x, y] : defaultValue;
                }
            }

            return newArr;
        }

        public static T[,] Offset<T>(this T[,] arr, int offsetX, int offsetY, T defaultValue = default)
        {
            int width = arr.GetLength(0);
            int depth = arr.GetLength(1);
            T[,] newArr = new T[width, depth];

            for (int y = 0; y < depth; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldX = x - offsetX;
                    int oldY = y - offsetY;
                    newArr[x, y] = oldX >= 0 && oldX < width && oldY >= 0 && oldY < depth ? arr[oldX, oldY] : defaultValue;
                }
            }

            return newArr;
        }
    }
}
