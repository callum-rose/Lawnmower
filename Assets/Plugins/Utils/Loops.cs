namespace Utils
{
    public static class Loops
    {
        public delegate void LoopBody2D(int x, int y);

        public static void TwoD(int width, int height, LoopBody2D body)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    body.Invoke(x, y);
                }
            }
        }
    }
}
