using System;

namespace Core
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class UnreferencedScriptableObjectAttribute : Attribute
	{
	}
}