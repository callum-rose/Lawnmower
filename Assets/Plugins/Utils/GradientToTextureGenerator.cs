#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plugins.Utils
{
	public static class GradientToTextureGenerator
	{
		public static Texture2D GenerateLut(Vector2Int textureSize, Gradient gradient, string outputAssetPath, bool askBeforeOverwrite = true, bool selectAssetWhenDone = true)
		{
			string fullPath = Path.Combine(Application.dataPath, outputAssetPath);

			if (askBeforeOverwrite)
			{
				if (File.Exists(fullPath))
				{
					if (!EditorUtility.DisplayDialog("Overwrite existing file?", fullPath, "Yes", "No"))
					{
						return null;
					}
				}
			}

			Texture2D texture = new Texture2D(textureSize.x, textureSize.y);
			for (int x = 0; x < texture.width; x++)
			{
				float t = (float) x / texture.width;
				Color colour = gradient.Evaluate(t);
				texture.SetPixels(x, 0, 1, texture.height, Enumerable.Repeat(colour, texture.height).ToArray());
			}

			texture.Apply();

			byte[] bytes = texture.EncodeToPNG();

			using (FileStream stream = File.OpenWrite(fullPath))
			{
				stream.Write(bytes, 0, bytes.Length);
			}

			AssetDatabase.Refresh();

			Texture2D createdTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + outputAssetPath);

			if (selectAssetWhenDone)
			{
				EditorUtility.FocusProjectWindow();
				Selection.activeObject = createdTexture;
			}

			return createdTexture;
		}
	}
}
#endif