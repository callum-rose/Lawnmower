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

		[ShowInInspector, ValidateInput(nameof(SetCurrent))]
		public Guid CurrentId { get; private set; }

		private void Awake()
		{
			CurrentId = new Guid();//PersistantData.Mower.CurrentId.Load())PersistantData.Mower.CurrentId.Load();
		}

		public MowerData GetCurrent()
		{
			return mowerDataManager.GetMowerData(CurrentId);
		}
		
		public bool SetCurrent(Guid id)
		{
			CurrentId = id;
			PersistantData.Mower.CurrentId.Save(CurrentId);
			return true;
		}
	}
}