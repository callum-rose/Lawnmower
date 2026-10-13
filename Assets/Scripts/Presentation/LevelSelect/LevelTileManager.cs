using System;
using System.Collections.Generic;
using BalsamicBits.Extensions;
using Lawnmower.Presenters;
using UnityEngine;

namespace LevelSelect
{
	/// <summary>The level list view: one tile per level; reports taps.</summary>
	internal class LevelTileManager : MonoBehaviour, ILevelSelectView
	{
		[SerializeField] private LevelTile levelTilePrefab;
		[SerializeField] private Transform tileContainer;

		public event Action<int> LevelChosen;

		/// <summary>Called while the level list canvas may still be inactive (it opens from the Play button).</summary>
		public void ShowLevels(IReadOnlyList<LevelSummary> levels)
		{
			// The scene's placeholder tiles.
			transform.DestroyAllChildren();

			foreach (LevelSummary level in levels)
			{
				LevelTile newTile = Instantiate(levelTilePrefab, tileContainer);
				newTile.Setup(level.Number, level.IsLocked);

				int index = level.Index;
				newTile.Clicked += () => LevelChosen?.Invoke(index);
			}
		}
	}
}
