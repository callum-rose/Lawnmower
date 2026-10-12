using Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Lawnmower.Editor.Inspector
{
	[CustomEditor(typeof(ViewManager))]
	internal class ViewManagerEditor : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			VisualElement root = new VisualElement();
			InspectorElement.FillDefaultInspector(root, serializedObject, this);

			ViewManager viewManager = (ViewManager)target;

			Button enterPlay = new Button(viewManager.EnterPlayModeFromEditor) { text = "Enter Play Mode" };
			Button loadScene = new Button(viewManager.LoadSelectedSceneFromEditor) { text = "Load Scene" };

			void Refresh()
			{
				enterPlay.SetEnabled(!EditorApplication.isPlaying);
				loadScene.SetEnabled(EditorApplication.isPlaying);
			}

			Refresh();
			root.schedule.Execute(Refresh).Every(500);

			VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
			enterPlay.style.flexGrow = 1;
			loadScene.style.flexGrow = 1;
			row.Add(enterPlay);
			row.Add(loadScene);
			root.Add(row);

			return root;
		}
	}
}
