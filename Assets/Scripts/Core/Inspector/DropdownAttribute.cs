using UnityEngine;

namespace Core
{
	/// <summary>
	/// Shows a string field as a dropdown. <see cref="ProviderName"/> names a field, property or
	/// parameterless method (instance or static) on the object that declares the field, returning
	/// <c>IEnumerable&lt;string&gt;</c>. Drawn by DropdownDrawer (UI Toolkit inspectors only).
	/// </summary>
	public class DropdownAttribute : PropertyAttribute
	{
		public string ProviderName { get; }

		public DropdownAttribute(string providerName)
		{
			ProviderName = providerName;
		}
	}
}
