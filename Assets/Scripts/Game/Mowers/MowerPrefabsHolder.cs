using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Core;

namespace Game.Mowers
{
    [CreateAssetMenu(fileName = "MowerPrefabsHolder", menuName = SONames.GameDir + "Mower Prefabs Holder")]
    internal class MowerPrefabsHolder : ScriptableObject
    {
        [SerializeField, InlineEditor] private List<MowerData> mowerData;

        public MowerManager GetPrefab(Guid id)
        {
            MowerData data = mowerData.First(md => md.Id == id);
            return data.Prefab;
        }

        public MowerManager GetFirstPrefab()
        {
            return GetPrefab(mowerData[0].Id);
        }
    }
}
