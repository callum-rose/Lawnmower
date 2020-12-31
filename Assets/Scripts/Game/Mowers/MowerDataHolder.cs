using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Core;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(MowerDataHolder), menuName = SONames.GameDir + nameof(MowerDataHolder))]
	internal class MowerDataHolder : ScriptableObject
	{
		[SerializeField, InlineEditor] private List<MowerData> mowerData;

		public MowerData GetMowerData(Guid id)
		{
			bool Match(MowerData md) => md.Id == id;

			if (!mowerData.Exists(Match))
			{
				return mowerData[0];
			}

			return mowerData.First(Match);
		}

		public MowerManager GetPrefab(Guid id)
		{
			return GetMowerData(id).Prefab;
		}
	}
}