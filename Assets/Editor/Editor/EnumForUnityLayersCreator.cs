#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Core
{
    internal class EnumForUnityLayersCreator : EnumCreator
    {
        private const string menuPath = "Callum/Create Enum For Unity Layers";
        protected override string MenuPath => menuPath;

        protected override string EnumName => "UnityLayers";

        [MenuItem(menuPath)]
        private static void OpenWindow()
        {
            CreateWindow<EnumForUnityLayersCreator>();
        }

        protected override IList<KeyValuePair<string, int?>> GetValues()
        {
            var values = new List<KeyValuePair<string, int?>>();

            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                values.Add(new KeyValuePair<string, int?>(name, i));
            }

            return values;
        }
    }
}
#endif
