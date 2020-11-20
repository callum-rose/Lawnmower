using UnityEngine;

namespace Utils
{
    public static class SpriteUtil
    {
        public static DisposableSprite Create(byte[] bytes)
        {
            Texture2D tex = new Texture2D(1, 1);
            if (bytes == null || bytes.Length == 0 || !tex.LoadImage(bytes))
            {
                return new DisposableSprite();
            }

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);

            return new DisposableSprite(sprite);
        }

        public static DisposableSprite FlipX(Sprite sprite)
        {
            if (sprite == null)
            {
                return new DisposableSprite();
            }

            Texture2D tex = sprite.texture;
            Texture2D newTex = new Texture2D(tex.width, tex.height);

            int flippedX;
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.width; y++)
                {
                    flippedX = tex.width - 1 - x;
                    newTex.SetPixel(x, y, tex.GetPixel(flippedX, y));
                }
            }

            newTex.Apply();

            Sprite newSprite = Sprite.Create(newTex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);

            return new DisposableSprite(sprite);
        }

        public static void Dispose(DisposableSprite disposableSprite)
        {
            disposableSprite?.Dispose();
        }
    }
}
