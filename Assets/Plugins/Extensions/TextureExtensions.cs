using UnityEngine;

namespace BalsamicBits.Extensions
{
    public static class TextureExtensions
    {
        public static Texture2D FlipXY(this Texture2D input, bool doApply = true)
        {
            Texture2D texFlippedX = input.FlipX(false);
            return input.FlipY(true);
        }

        public static Texture2D FlipX(this Texture2D input, bool doApply = true)
        {
            Texture2D output = new Texture2D(input.width, input.height);

            Color[] colourColumn;
            for (int i = 0; i < input.width; i++)
            {
                colourColumn = input.GetPixels(i, 0, 1, input.height);
                output.SetPixels(input.width - i - 1, 0, 1, input.height, colourColumn);
            }

            if (doApply)
            {
                output.Apply();
            }

            return output;
        }

        public static Texture2D FlipY(this Texture2D input, bool doApply = true)
        {
            Texture2D output = new Texture2D(input.width, input.height);

            Color[] colourRow;
            for (int j = 0; j < input.height; j++)
            {
                colourRow = input.GetPixels(0, j, input.width, 1);
                output.SetPixels(0, input.height - j - 1, input.width, 1, colourRow);
            }

            if (doApply)
            {
                output.Apply();
            }

            return output;
        }

        public static Texture2D FlipX(this WebCamTexture input, bool doApply = true)
        {
            Texture2D output = new Texture2D(input.width, input.height);
            // mirror about x, to account for raw image scale x == -1
            Color[] colourColumn;
            for (int i = 0; i < input.width; i++)
            {
                colourColumn = input.GetPixels(i, 0, 1, input.height);
                output.SetPixels(input.width - i - 1, 0, 1, input.height, colourColumn);
            }

            if (doApply)
            {
                output.Apply();
            }

            return output;
        }

        public static Texture2D FlipY(this WebCamTexture input, bool doApply = true)
        {
            Texture2D output = new Texture2D(input.width, input.height);

            Color[] colourRow;
            for (int j = 0; j < input.height; j++)
            {
                colourRow = input.GetPixels(0, j, input.width, 1);
                output.SetPixels(0, input.height - j - 1, input.width, 1, colourRow);
            }

            if (doApply)
            {
                output.Apply();
            }

            return output;
        }

        public static Texture2D ApplyMaterial(this Texture input, Material mat, bool doApply = true)
        {
            RenderTexture filteredSnap = new RenderTexture(input.width, input.height, 1);

            Graphics.Blit(input, filteredSnap, mat);

            RenderTexture.active = filteredSnap;

            Texture2D output = new Texture2D(filteredSnap.width, filteredSnap.height);
            output.ReadPixels(new Rect(0, 0, filteredSnap.width, filteredSnap.height), 0, 0);

            if (doApply)
            {
                output.Apply();
            }

            return output;
        }
    }
}
