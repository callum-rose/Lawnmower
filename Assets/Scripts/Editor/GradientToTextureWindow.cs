using System.IO;
using Plugins.Utils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	public class GradientToTextureWindow : OdinEditorWindow
	{
		[SerializeField] private Gradient gradient;
		[SerializeField] private Vector2Int textureSize = new Vector2Int(256, 1);

		[SerializeField, FolderPath(RequireExistingPath = true, ParentFolder = "Assets")]
		private string outputPath;

		[SerializeField] private string outputName = "lut";

		[MenuItem("Callum/Gradient To Texture Generator Window")]
		private static void Open()
		{
			var window = CreateWindow<GradientToTextureWindow>();
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 400);
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			outputPath = "Textures";
		}

		[Button]
		private void Generate()
		{
			string path = Path.Combine(outputPath, outputName + ".png");
			GradientToTextureGenerator.GenerateLut(textureSize, gradient, path);
		}
	}
}