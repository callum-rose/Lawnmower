using System;
using Core;
using UnityEngine;

namespace Game.Mowers
{
    [CreateAssetMenu(fileName = nameof(CurrentMowerManager), menuName = SONames.GameDir + nameof(CurrentMowerManager))]
    public class CurrentMowerManager : ScriptableObject
    {
        [SerializeField] private MowerDataHolder mowerDataHolder;

        public Guid CurrentId { get; private set; }

        private void Awake()
        {
            CurrentId = PersistantData.Mower.CurrentId.Load();
        }

        public MowerData GetCurrent()
        {
            return mowerDataHolder.GetMowerData(CurrentId);
        }
        
        public void SetCurrent(Guid id)
        {
            CurrentId = id;
            PersistantData.Mower.CurrentId.Save(CurrentId);
        }
    }
}
