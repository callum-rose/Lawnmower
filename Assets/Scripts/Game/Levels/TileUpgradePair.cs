using System;
using System.Collections.Generic;
using System.Linq;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
	[Serializable]
	internal class TileUpgradePair
	{
		[LabelText("This tile"), SerializeField, ValueDropdown(nameof(PossibleTileConfigurations), HideChildProperties = true)]
		private string upgradee = NoneStr;

		[LabelText("Upgrades To"), SerializeField, ValueDropdown(nameof(PossibleTileConfigurations), HideChildProperties = true)]
		private string upgradeTo = NoneStr;

		private const string NoneStr = "None";

		private IEnumerable<string> PossibleTileConfigurations
		{
			get
			{
				List<string> ret = new List<string> { NoneStr };
				ret.AddRange(TileStatics.AllTileConfigurations.Select(t => t.ToString()));
				return ret;
			}
		}

		public bool IsMatch(Tile tileToUpgrade, out Tile tileToUpgradeTo)
		{
			string tileToUpgradeString = tileToUpgrade?.ToString() ?? NoneStr;
			if (!tileToUpgradeString.Equals(upgradee))
			{
				tileToUpgradeTo = null;
				return false;
			}

			tileToUpgradeTo = TileStatics.AllTileConfigurations.First(t => t.ToString().Equals(upgradeTo)).Clone();
			return true;
		}
	}
}