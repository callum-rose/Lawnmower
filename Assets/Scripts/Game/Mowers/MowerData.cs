using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Core;

namespace Game.Mowers
{
    [CreateAssetMenu(fileName = "Mower", menuName = SONames.GameDir + "Mower")]
    internal class MowerData : SerializedScriptableObject
    {
        [SerializeField] private Guid id;
        [SerializeField] private string name;
        [SerializeField, AssetsOnly] private MowerManager prefab;

        public Guid Id => id;
        public string Name => name;
        public MowerManager Prefab => prefab;

        private void Awake()
        {
            if (id != Guid.Empty)
            {
                return;
            }

            id = Guid.NewGuid();
        }
    }
}
