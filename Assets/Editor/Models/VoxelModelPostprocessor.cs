using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the voxel-style FBXs (Assets/Docs/VoxelExport.md): keeps the per-corner vertex data intact,
/// loops the creatures' baked animation and swaps each embedded <c>Palette_&lt;hex&gt;</c> material for a shared
/// <c>Game/VoxelProp</c> material of that colour in Assets/Materials/Game/Environment/Palette.
/// </summary>
internal class VoxelModelPostprocessor : AssetPostprocessor
{
	private static readonly string[] staticFolders = { "Assets/Models/Pond/", "Assets/Models/Tiles/" };
	private const string animatedFolder = "Assets/Models/Creatures/";
	private const string paletteFolder = "Assets/Materials/Game/Environment/Palette";
	private const string palettePrefix = "Palette_";
	private const string shaderName = "Game/VoxelProp";

	private static bool IsVoxelModel(string path)
	{
		if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return System.Array.Exists(staticFolders, path.StartsWith) || path.StartsWith(animatedFolder);
	}

	private void OnPreprocessModel()
	{
		if (!IsVoxelModel(assetPath))
		{
			return;
		}

		ModelImporter importer = (ModelImporter)assetImporter;
		importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
		importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
		importer.optimizeMeshPolygons = false;
		importer.optimizeMeshVertices = false;
		importer.meshCompression = ModelImporterMeshCompression.Off;
		importer.generateSecondaryUV = false;
		importer.weldVertices = false;
		importer.importCameras = false;
		importer.importLights = false;
		importer.importBlendShapes = false;
		importer.preserveHierarchy = true;
		bool animated = assetPath.StartsWith(animatedFolder);
		importer.animationType = animated ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
		importer.importAnimation = animated;
	}

	private void OnPreprocessAnimation()
	{
		if (!assetPath.StartsWith(animatedFolder) || !IsVoxelModel(assetPath))
		{
			return;
		}

		ModelImporter importer = (ModelImporter)assetImporter;
		ModelImporterClipAnimation[] clips = importer.clipAnimations;

		if (clips.Length == 0)
		{
			clips = importer.defaultClipAnimations;
		}

		foreach (ModelImporterClipAnimation clip in clips)
		{
			clip.loopTime = true;
			clip.wrapMode = WrapMode.Loop;
		}

		importer.clipAnimations = clips;
	}

	private Material OnAssignMaterialModel(Material material, Renderer renderer)
	{
		if (!IsVoxelModel(assetPath) || !material.name.StartsWith(palettePrefix))
		{
			return null;
		}

		string path = $"{paletteFolder}/{material.name}.mat";
		context.DependsOnSourceAsset(path);
		Material palette = AssetDatabase.LoadAssetAtPath<Material>(path);

		if (palette == null)
		{
			Debug.LogWarning($"{assetPath}: no palette material at {path}. Create it with the {shaderName} shader, " +
				"_Tint set to the hex colour, then reimport.");
		}

		return palette;
	}
}
