using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Core;

namespace Game.Mowers
{
    [CreateAssetMenu(fileName = "Mower", menuName = SoNames.GameDir + "Mower")]
    public class MowerData : SerializedScriptableObject
    {
        [SerializeField] private Guid id;
        // Unity-serialized replacement for the Odin-serialized id (populated by the Odin data migration).
        [SerializeField] internal string guid;
        [SerializeField] private string name;
        [SerializeField, AssetsOnly] private MowerObject prefab;

        public Guid Id => id;
        internal Guid LegacyId => id;
        public string Name => name;
        public MowerObject Prefab => prefab;

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
