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
        [SerializeField] private string name;
        [SerializeField, AssetsOnly] private MowerObject prefab;

        public Guid Id => id;
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
