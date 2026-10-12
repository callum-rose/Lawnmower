using Game.Levels;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Lawnmower.Editor.Inspector
{
	[CustomEditor(typeof(LevelDataManager))]
	internal class LevelDataManagerEditor : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			LevelDataManager manager = (LevelDataManager)target;

			VisualElement root = new VisualElement();
			InspectorElement.FillDefaultInspector(root, serializedObject, this);

			Label notIncludedHeader = new Label("Levels not included") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, marginTop = 8 } };
			VisualElement notIncluded = new VisualElement { style = { marginLeft = 4 } };

			void RefreshList()
			{
				manager.UpdateNotIncludedLevels();
				notIncluded.Clear();
				if (manager.NotIncludedLevels.Count == 0)
				{
					notIncluded.Add(new Label("(none)"));
				}

				foreach (LevelData level in manager.NotIncludedLevels)
				{
					ObjectField field = new ObjectField { objectType = typeof(LevelData), value = level };
					field.SetEnabled(false);
					notIncluded.Add(field);
				}
			}

			Button update = new Button(RefreshList) { text = "Update Not Included Levels" };
			Button add = new Button(() =>
			{
				Undo.RecordObject(manager, "Add Not Included Levels");
				manager.AddNotIncludedLevels();
				serializedObject.Update();
				RefreshList();
			}) { text = "Add Not Included Levels" };

			VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
			update.style.flexGrow = 1;
			add.style.flexGrow = 1;
			row.Add(update);
			row.Add(add);

			root.Add(notIncludedHeader);
			root.Add(notIncluded);
			root.Add(row);

			RefreshList();
			return root;
		}
	}
}
