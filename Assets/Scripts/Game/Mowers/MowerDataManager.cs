using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Core;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(MowerDataManager), menuName = SoNames.GameDir + nameof(MowerDataManager))]
	internal class MowerDataManager : ScriptableObject
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
	}
}