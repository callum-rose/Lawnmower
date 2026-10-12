using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lawnmower.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(DropdownAttribute))]
	internal class DropdownDrawer : PropertyDrawer
	{
		private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
		                                   BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				return new HelpBox($"[Dropdown] only supports string fields ({property.displayName})", HelpBoxMessageType.Error);
			}

			List<string> choices = GetChoices(property);
			string current = property.stringValue;
			if (!string.IsNullOrEmpty(current) && !choices.Contains(current))
			{
				choices.Insert(0, current);
			}

			DropdownField field = new DropdownField(property.displayName, choices, current ?? string.Empty);
			field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
			field.BindProperty(property);
			field.tooltip = property.tooltip;
			return field;
		}

		private List<string> GetChoices(SerializedProperty property)
		{
			string providerName = ((DropdownAttribute)attribute).ProviderName;
			try
			{
				object owner = GetDeclaringObject(property);
				Type type = fieldInfo.DeclaringType;
				object value = null;

				for (Type t = type; t != null && value == null; t = t.BaseType)
				{
					if (t.GetField(providerName, Flags) is FieldInfo f)
					{
						value = f.GetValue(f.IsStatic ? null : owner);
					}
					else if (t.GetProperty(providerName, Flags) is PropertyInfo p)
					{
						value = p.GetValue(p.GetGetMethod(true).IsStatic ? null : owner);
					}
					else if (t.GetMethod(providerName, Flags, null, Type.EmptyTypes, null) is MethodInfo m)
					{
						value = m.Invoke(m.IsStatic ? null : owner, null);
					}
				}

				return value is IEnumerable enumerable
					? enumerable.Cast<object>().Select(o => o?.ToString() ?? string.Empty).ToList()
					: new List<string>();
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[Dropdown] provider '{providerName}' failed for {property.propertyPath}: {e.Message}");
				return new List<string>();
			}
		}

		/// <summary>Walks the property path to find the object that declares the field (supports nested serializable classes and arrays).</summary>
		private static object GetDeclaringObject(SerializedProperty property)
		{
			object current = property.serializedObject.targetObject;
			string[] parts = property.propertyPath.Replace(".Array.data[", "[").Split('.');

			for (int i = 0; i < parts.Length - 1 && current != null; i++)
			{
				string part = parts[i];
				int bracket = part.IndexOf('[');
				if (bracket >= 0)
				{
					string name = part.Substring(0, bracket);
					int index = int.Parse(part.Substring(bracket + 1, part.Length - bracket - 2));
					current = GetMember(current, name) is IList list && index < list.Count ? list[index] : null;
				}
				else
				{
					current = GetMember(current, part);
				}
			}

			return current;
		}

		private static object GetMember(object source, string name)
		{
			for (Type t = source.GetType(); t != null; t = t.BaseType)
			{
				FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (f != null)
				{
					return f.GetValue(source);
				}
			}

			return null;
		}
	}
}
