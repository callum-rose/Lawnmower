using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
	[Serializable]
	internal class TileUpgradePair
	{
		[SerializeField, Dropdown(nameof(PossibleTileConfigurations))]
		private string upgradee = NoneStr;

		[SerializeField, Dropdown(nameof(PossibleTileConfigurations))]
		private string upgradeTo = NoneStr;

		private const string NoneStr = "None";

		private IEnumerable<string> PossibleTileConfigurations
		{
			get
			{
				List<string> ret = new List<string> { NoneStr };
				ret.AddRange(TileStatics.AllTileConfigurations.Select(t => t.ConfigurationKey));
				return ret;
			}
		}

		// Tiles are matched by their stable ConfigurationKey (e.g. "grass:3"), not ToString(),
		// so renaming a tile class or namespace doesn't silently break saved pairs.
		public bool IsMatch(Tile tileToUpgrade, out Tile tileToUpgradeTo)
		{
			string tileToUpgradeKey = tileToUpgrade?.ConfigurationKey ?? NoneStr;
			if (!tileToUpgradeKey.Equals(upgradee))
			{
				tileToUpgradeTo = null;
				return false;
			}

			Tile template = TileStatics.AllTileConfigurations.FirstOrDefault(t => t.ConfigurationKey.Equals(upgradeTo));
			if (template == null)
			{
				throw new InvalidOperationException($"No tile configuration with key '{upgradeTo}' to upgrade '{upgradee}' to");
			}

			tileToUpgradeTo = template.Clone();
			return true;
		}
	}
}