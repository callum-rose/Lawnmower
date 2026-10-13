using Game.Cameras;
using Game.Core;
using Game.Input;
using Game.Levels;
using Game.UI.Screens;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lawnmower.Editor.GameScreens
{
	/// <summary>
	/// One-off (idempotent) Game scene migration to the Bunny Heaven screens: removes the old HUD canvas, tutorial canvas
	/// and 3D arrows, places GameScreen.prefab, and adds the board-framing camera.
	/// </summary>
	public static class GameSceneInstaller
	{
		private const string ScenePath = "Assets/Scenes/Game.unity";

		[MenuItem("Bunny Heaven/Game Screens/Install In Game Scene")]
		public static string Install()
		{
			Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
			var log = new System.Text.StringBuilder();

			foreach (string path in new[] { "UI/Canvas", "UI/TutorialCanvas", "ArrowsContainer" })
			{
				GameObject old = Find(scene, path);
				if (old != null)
				{
					Object.DestroyImmediate(old);
					log.AppendLine($"removed {path}");
				}
			}

			GameObject uiRoot = Find(scene, "UI");
			GameScreenView screen = Object.FindAnyObjectByType<GameScreenView>(FindObjectsInactive.Include);
			if (screen == null)
			{
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameScreenBuilder.PrefabPath);
				var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
				instance.transform.SetParent(uiRoot.transform, false);
				screen = instance.GetComponent<GameScreenView>();
				log.AppendLine("placed GameScreen");
			}

			var game = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
			Camera gameCamera = Find(scene, "Camera/GameCamera").GetComponent<Camera>();
			SetRefs(game, ("screen", screen), ("gameCamera", gameCamera));

			var input = Object.FindAnyObjectByType<MoveInputController>(FindObjectsInactive.Include);
			SetRefs(input, ("arrows", screen.ArrowPad));

			GameObject cameraRoot = Find(scene, "Camera");
			Transform boardCamTransform = cameraRoot.transform.Find("BoardCam");
			if (boardCamTransform == null)
			{
				var boardCamGo = new GameObject("BoardCam");
				boardCamGo.transform.SetParent(cameraRoot.transform, false);
				boardCamTransform = boardCamGo.transform;
				log.AppendLine("added BoardCam");
			}

			var boardCam = boardCamTransform.GetComponent<CinemachineCamera>() ?? boardCamTransform.gameObject.AddComponent<CinemachineCamera>();
			boardCam.Priority = 100;
			LensSettings lens = boardCam.Lens;
			lens.FieldOfView = 7;
			lens.NearClipPlane = 10;
			lens.FarClipPlane = 600;
			boardCam.Lens = lens;

			var framer = boardCamTransform.GetComponent<BoardCameraFramer>() ?? boardCamTransform.gameObject.AddComponent<BoardCameraFramer>();
			SetRefs(framer,
				("boardCamera", boardCam),
				("levelManager", Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include)),
				("positioner", Object.FindAnyObjectByType<Positioner>(FindObjectsInactive.Include)),
				("screen", screen));

			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			return log.ToString();
		}

		private static GameObject Find(Scene scene, string path)
		{
			string[] parts = path.Split('/');
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				if (root.name != parts[0])
				{
					continue;
				}

				Transform t = root.transform;
				for (int i = 1; i < parts.Length && t != null; i++)
				{
					t = t.Find(parts[i]);
				}

				if (t != null)
				{
					return t.gameObject;
				}
			}

			return null;
		}

		private static void SetRefs(Object target, params (string field, Object value)[] refs)
		{
			var so = new SerializedObject(target);
			foreach ((string field, Object value) in refs)
			{
				SerializedProperty property = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name}.{field}");
				property.objectReferenceValue = value;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
		}
	}
}
