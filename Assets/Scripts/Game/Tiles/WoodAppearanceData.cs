using System.Collections.Generic;
using System.IO;
using BalsamicBits.Extensions;
using Core;
using Plugins.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(WoodAppearanceData), menuName = SONames.GameDir + nameof(WoodAppearanceData))]
	internal class WoodAppearanceData : ScriptableObject
	{
		#region Unity

		private void OnEnable()
		{
			outputPath = "Textures";

			Shader test;
		}

		#endregion

		#region Input

		[TitleGroup("Input")] [SerializeField] private Vector2Int textureSize = new Vector2Int(256, 4);
		[SerializeField] private Gradient highGradientLut, lowGradientLut;

		[SerializeField, FolderPath(RequireExistingPath = true, ParentFolder = "Assets")]
		private string outputPath;

		[SerializeField] private string outputName = "lut";

		[SerializeField] private bool askBeforeOverwrite = true;

#if UNITY_EDITOR
		[TitleGroup("Input")]
		[Button]
		private void Generate()
		{
			highLut = GradientToTextureGenerator.GenerateLut(textureSize, highGradientLut, GetFullPath("high"),
				askBeforeOverwrite, false);
			lowLut = GradientToTextureGenerator.GenerateLut(textureSize, lowGradientLut, GetFullPath("low"),
				askBeforeOverwrite, false);
		}
#endif

		#endregion

		#region Output

		[TitleGroup("Output")] [SerializeField]
		private Texture2D highLut, lowLut;

		#endregion

		#region Material

		[TitleGroup("Material")] [SerializeField]
		private Material woodMaterial;

		[SerializeField, DisableIf("@" + nameof(woodMaterial) + " == null"),
		 ValueDropdown(nameof(WoodShaderProperties), HideChildProperties = true)]
		private string highLutShaderPropertyName, lowLutShaderPropertyName;

		private List<string> WoodShaderProperties
		{
			get
			{
				if (woodMaterial == null)
				{
					return new List<string>();
				}

				return woodMaterial.shader.GetPropertyNames();
			}
		}

		[Button, DisableIf("@" + nameof(woodMaterial) + " == null")]
		private void AssignLutsToMaterial()
		{
			woodMaterial.SetTexture(highLutShaderPropertyName, highLut);
			woodMaterial.SetTexture(lowLutShaderPropertyName, lowLut);
		}

		#endregion

		private string GetFullPath(string suffix)
		{
			return Path.Combine(outputPath, outputName + "_" + suffix + ".png");
		}
	}
}