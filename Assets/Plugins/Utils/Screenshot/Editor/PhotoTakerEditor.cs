#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Utils.Screenshot.Editor
{
    [CustomEditor(typeof(PhotoTaker))]
    public class PhotoTakerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PhotoTaker photoTaker = target as PhotoTaker;

            base.OnInspectorGUI();

            GUILayout.BeginVertical();

            if (GUILayout.Button("Take Screenshot"))
            {
                photoTaker.TakeScreenShot();
            }

            GUILayout.EndVertical();
        }
    }
}
#endif