#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Assertions;

namespace Core
{
    public static class ViewManagerSelector 
    {
        [MenuItem("Callum/Select " + nameof(ViewManager))]
        public static void ShowViewManagerInProject()
        {
            var ids = AssetDatabase.FindAssets("t:" + nameof(ViewManager));
            Assert.IsTrue(ids.Length == 1);

            string assetPath = AssetDatabase.GUIDToAssetPath(ids[0]);
            ViewManager v = AssetDatabase.LoadAssetAtPath<ViewManager>(assetPath);

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = v;
        }
    }
}
#endif