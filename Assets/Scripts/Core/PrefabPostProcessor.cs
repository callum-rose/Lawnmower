// using Game.Core;
// using UnityEditor;
// using UnityEngine;
//
// public class PrefabPostProcessor : AssetPostprocessor
// {
// 	private void OnPostprocessPrefab(GameObject g)
// 	{
// 		PostProcessRemover[] pprs = g.GetComponentsInChildren<PostProcessRemover>();
//
// 		if (pprs == null || pprs.Length == 0)
// 		{
// 			return;
// 		}
//
// 		foreach (PostProcessRemover p in pprs)
// 		{
// 			p.RemoveAllThenSelf();
// 		}
// 	}
// }