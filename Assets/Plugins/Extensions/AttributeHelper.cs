using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BalsamicBits.Extensions
{
	public static class AttributeHelper
	{
		public static IEnumerable<Type> GetTypesWithAttribute<T>(Assembly assembly) where T : Attribute
		{
			return assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(T), true).Length > 0);
		}
	}
}