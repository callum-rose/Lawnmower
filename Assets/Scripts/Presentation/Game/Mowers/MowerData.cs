using System;
using UnityEngine;
using Core;

namespace Game.Mowers
{
    [CreateAssetMenu(fileName = "Mower", menuName = SoNames.GameDir + "Mower")]
    public class MowerData : ScriptableObject
    {
        /// <summary>Mower id in Guid "N" format. Persisted as the player's current mower, so never change it.</summary>
        [SerializeField] private string guid;
        [SerializeField] private string name;
        [SerializeField] private MowerObject prefab;

        public Guid Id => Guid.TryParse(guid, out Guid g) ? g : Guid.Empty;
        public string Name => name;
        public MowerObject Prefab => prefab;

#if UNITY_EDITOR
        private void Reset()
        {
            guid = Guid.NewGuid().ToString("N");
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
