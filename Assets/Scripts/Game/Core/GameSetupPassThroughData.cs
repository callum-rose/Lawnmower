using Core;
using Game.Levels;
using Game.Mowers;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    internal class GameSetupPassThroughData : PassThroughData
    {
        [HideInInspector] public Guid? MowerId { get; set; }
        [SerializeField] public LevelData Level { get; set; }

#if UNITY_EDITOR
        [SerializeField, OnValueChanged(nameof(OnMowerDataValueChanged))] private MowerData mowerData;
        private bool MowerIdNonNull => MowerId != null;
        [ShowInInspector, ReadOnly, ShowIf(nameof(MowerIdNonNull)), LabelText("Mower Id"), DisplayAsString]
        private Guid IdToDisplay => MowerId ?? Guid.Empty;

        private void OnMowerDataValueChanged(MowerData mowerData)
        {
            MowerId = mowerData?.Id ?? Guid.Empty;
        }
#endif
    }
}
