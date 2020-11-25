#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Tiles
{
    public class TileDataDrawer : OdinValueDrawer<TileData>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Rect rect = EditorGUILayout.GetControlRect();

            if (label != null)
            {
                rect = EditorGUI.PrefixLabel(rect, label);
            }

            TileData value = ValueEntry.SmartValue;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical();

            GUIHelper.PushLabelWidth(20);
            value.Type = (TileType)EditorGUI.EnumPopup(rect.AlignTop(40), value.Type);

            switch (value.Type)
            {
                case TileType.Grass:
                    if (value.Data == null || value.Data.GetType() != typeof(GrassTileSetupData))
                    {
                        value.Data = new GrassTileSetupData(0);
                    }

                    GrassTileSetupData grassData = value.Data as GrassTileSetupData;
                    GUIHelper.PushLabelWidth(100);
                    grassData.grassHeight = EditorGUILayout.IntSlider(nameof(grassData.grassHeight), grassData.grassHeight, 1, GrassTile.MaxGrassHeight);

                    GUIHelper.PopLabelWidth();
                    break;

                case TileType.Stone:
                default:
                    value.Data = null;
                    break;
            }

            EditorGUILayout.EndVertical();

            GUIHelper.PopLabelWidth();

            if (EditorGUI.EndChangeCheck())
            {
                value.UpdateJsonData();
            }

            ValueEntry.SmartValue = value;
        }
    }
}
#endif
