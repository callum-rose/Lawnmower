using System;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(CurrentMowerManager), menuName = SONames.GameDir + nameof(CurrentMowerManager))]
	public class CurrentMowerManager : ScriptableObject
	{
		[FormerlySerializedAs("mowerDataHolder")] [SerializeField]
		private MowerDataManager mowerDataManager;

		[ShowInInspector, ReadOnly]
		public Guid CurrentId { get; private set; }

		private void Awake()
		{
			CurrentId = PersistantData.Mower.CurrentId.Load();
		}

		public MowerData GetCurrent()
		{
			return mowerDataManager.GetMowerData(CurrentId);
		}

		public void SetCurrent(Guid id)
		{
			CurrentId = id;
			PersistantData.Mower.CurrentId.Save(CurrentId);
		}
	}
}