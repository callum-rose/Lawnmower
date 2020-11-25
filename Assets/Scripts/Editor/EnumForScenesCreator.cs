#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Text;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Collections.Generic;

namespace Core
{
    internal class EnumForScenesCreatorWindow : EnumCreator
    {
        private const string menuPath = "Callum/" + nameof(EnumForScenesCreatorWindow);
        protected override string MenuPath => "Callum/" + nameof(EnumForScenesCreatorWindow);
        protected override string EnumName => "UnityScene";

        [MenuItem(menuPath)]
        private static void CreateWindow()
        {
            CreateWindow<EnumForScenesCreatorWindow>();
        }

        protected override IList<KeyValuePair<string, int?>> GetValues()
        {
            var values = new List<KeyValuePair<string, int?>>();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
                string name = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(scene.guid.ToString())).name;

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
