using Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lawnmower.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
	internal class MinMaxRangeDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.Vector2)
			{
				return new HelpBox($"[MinMaxRange] only supports Vector2 fields ({property.displayName})", HelpBoxMessageType.Error);
			}

			MinMaxRangeAttribute range = (MinMaxRangeAttribute)attribute;

			VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
			MinMaxSlider slider = new MinMaxSlider(property.displayName, property.vector2Value.x, property.vector2Value.y,
				range.Min, range.Max) { style = { flexGrow = 1 } };
			slider.AddToClassList(BaseField<Vector2>.alignedFieldUssClassName);
			slider.BindProperty(property);

			FloatField min = new FloatField { style = { width = 48 } };
			FloatField max = new FloatField { style = { width = 48 } };
			min.SetValueWithoutNotify(property.vector2Value.x);
			max.SetValueWithoutNotify(property.vector2Value.y);

			slider.RegisterValueChangedCallback(e =>
			{
				min.SetValueWithoutNotify(e.newValue.x);
				max.SetValueWithoutNotify(e.newValue.y);
			});
			min.RegisterValueChangedCallback(e => slider.value = new Vector2(Mathf.Clamp(e.newValue, range.Min, slider.value.y), slider.value.y));
			max.RegisterValueChangedCallback(e => slider.value = new Vector2(slider.value.x, Mathf.Clamp(e.newValue, slider.value.x, range.Max)));

			row.Add(slider);
			row.Add(min);
			row.Add(max);
			return row;
		}
	}
}
