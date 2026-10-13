using BunnyHeaven.UI;
using BunnyHeaven.UI.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lawnmower.Screens.Editor
{
	/// <summary>Small helpers for building screen prefabs out of the Bunny Heaven kit (sizes in Figma points).</summary>
	internal static class ScreenBuilderUtil
	{
		public const string KitDir = "Assets/Prefabs/UI/BunnyHeaven";

		public static readonly Vector4 Overhang = new Vector4(8, 8, 8, 12);

		public static UiTheme Theme => KitAssets.Theme;

		public static RectTransform NewUI(string name, Transform parent, Vector2? size = null)
		{
			GameObject go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
			RectTransform rect = (RectTransform)go.transform;
			if (parent != null)
			{
				rect.SetParent(parent, false);
			}

			if (size.HasValue)
			{
				rect.sizeDelta = size.Value;
			}

			return rect;
		}

		public static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = new Vector2(left, bottom);
			rect.offsetMax = new Vector2(-right, -top);
		}

		/// <summary>Places a rect by its Figma box, measured from the parent's top-left.</summary>
		public static void TopLeft(RectTransform rect, float x, float y, float w, float h)
		{
			rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
			rect.pivot = new Vector2(0, 1);
			rect.sizeDelta = new Vector2(w, h);
			rect.anchoredPosition = new Vector2(x, -y);
		}

		/// <summary>Horizontally centred on the parent, <paramref name="y"/> from its top.</summary>
		public static void TopCentre(RectTransform rect, float y, float w, float h)
		{
			rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
			rect.pivot = new Vector2(0.5f, 1);
			rect.sizeDelta = new Vector2(w, h);
			rect.anchoredPosition = new Vector2(0, -y);
		}

		public static void TopRight(RectTransform rect, float right, float y, float w, float h)
		{
			rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
			rect.pivot = new Vector2(1, 1);
			rect.sizeDelta = new Vector2(w, h);
			rect.anchoredPosition = new Vector2(-right, -y);
		}

		public static void Fixed(RectTransform rect, float width, float height)
		{
			LayoutElement element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
			element.minWidth = element.preferredWidth = width;
			element.minHeight = element.preferredHeight = height;
			rect.sizeDelta = new Vector2(width, height);
		}

		/// <summary>An Image tinted from the theme; <paramref name="themed"/> false leaves the colour to code.</summary>
		public static Image AddImage(Transform parent, string name, Sprite sprite, ThemeColor token, bool sliced = false,
			bool themed = true)
		{
			RectTransform rect = NewUI(name, parent);
			Image image = rect.gameObject.AddComponent<Image>();
			image.sprite = sprite;
			image.raycastTarget = false;
			if (sliced)
			{
				image.type = Image.Type.Sliced;
				image.pixelsPerUnitMultiplier = 1f;
			}

			image.color = Theme.Get(token);
			if (themed)
			{
				ThemedGraphic graphic = rect.gameObject.AddComponent<ThemedGraphic>();
				graphic.Theme = Theme;
				graphic.Token = token;
			}

			return image;
		}

		public static Image AddIcon(Transform parent, string name, string icon, float size, ThemeColor token, bool themed = true)
		{
			Image image = AddImage(parent, name, KitAssets.Icon(icon), token, themed: themed);
			image.preserveAspect = true;
			image.rectTransform.sizeDelta = new Vector2(size, size);
			return image;
		}

		/// <summary>A frame layer that hangs over its parent by <paramref name="overhang"/> (left, top, right, bottom).</summary>
		public static Image AddFrameLayer(Transform parent, string name, string frame, ThemeColor token, Vector4 overhang,
			bool themed = true)
		{
			bool sliced = frame is not ("Circle" or "Chip" or "StatCircle" or "Circle_Shadow");
			Image image = AddImage(parent, name, KitAssets.Sprite($"Frames/Frame_{frame}"), token, sliced, themed);
			Stretch(image.rectTransform, -overhang.x, -overhang.y, -overhang.z, -overhang.w);
			image.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
			return image;
		}

		public static TextMeshProUGUI AddText(Transform parent, string name, string text, ThemeFont font, float size,
			ThemeColor color, float spacingPercent, bool themedColor = true, bool inkShadow = false)
		{
			RectTransform rect = NewUI(name, parent);
			TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.fontSize = size;
			tmp.characterSpacing = spacingPercent;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.textWrappingMode = TextWrappingModes.NoWrap;
			tmp.overflowMode = TextOverflowModes.Overflow;
			tmp.raycastTarget = false;
			tmp.color = Theme.Get(color);

			ThemedText themedText = rect.gameObject.AddComponent<ThemedText>();
			themedText.Theme = Theme;
			themedText.Font = font;
			themedText.InkShadow = inkShadow;

			if (themedColor)
			{
				ThemedGraphic graphic = rect.gameObject.AddComponent<ThemedGraphic>();
				graphic.Theme = Theme;
				graphic.Token = color;
			}

			return tmp;
		}

		public static T Kit<T>(string prefabName, Transform parent) where T : Component
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitDir}/{prefabName}.prefab");
			GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
			return instance.GetComponent<T>();
		}

		/// <summary>The small white round button with an ink chevron (Level Select / Settings top-left).</summary>
		public static ButtonView AddBackButton(Transform parent)
		{
			ButtonView back = Kit<ButtonView>("RoundIconButton", parent);
			back.name = "BackButton";
			back.SetColor(ButtonColor.White);
			back.SetIcon(KitAssets.Icon("back"));
			back.Icon.rectTransform.sizeDelta = new Vector2(22, 22);
			back.Label.gameObject.SetActive(false);
			RectTransform circle = (RectTransform)back.transform.Find("Circle");
			Fixed(circle, 48, 48);
			RectTransform rect = (RectTransform)back.transform;
			TopLeft(rect, 20, 6, 48, 48);
			return back;
		}

		/// <summary>Records script-made changes on every nested prefab instance, so saving keeps them as overrides.</summary>
		public static void RecordInstanceChanges(GameObject root)
		{
			foreach (Component component in root.GetComponentsInChildren<Component>(true))
			{
				if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
				{
					PrefabUtility.RecordPrefabInstancePropertyModifications(component);
				}
			}

			foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
			{
				if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
				{
					PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
				}
			}
		}

		public static void SetRefs(Object target, params (string field, Object value)[] refs)
		{
			SerializedObject so = new SerializedObject(target);
			foreach ((string field, Object value) in refs)
			{
				SerializedProperty property = so.FindProperty(field);
				if (property == null)
				{
					throw new System.ArgumentException($"{target.GetType().Name} has no field '{field}'");
				}

				property.objectReferenceValue = value;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
		}

		public static GameObject Save(GameObject root, string path)
		{
			RecordInstanceChanges(root);
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
			Object.DestroyImmediate(root);
			return prefab;
		}
	}
}
