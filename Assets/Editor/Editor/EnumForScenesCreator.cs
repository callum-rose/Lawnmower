#if UNITY_EDITOR
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
            List<KeyValuePair<string, int?>> values = new List<KeyValuePair<string, int?>>();
            
            values.Add(new KeyValuePair<string, int?>("None", -1));

            int sceneIndex = 0;
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];

                if (!scene.enabled)
                {
                    continue;
                }
                
                string name = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(scene.guid.ToString())).name;

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                values.Add(new KeyValuePair<string, int?>(name, sceneIndex));
                
                sceneIndex++;
            }

            return values;
        }
    }
}
#endif
