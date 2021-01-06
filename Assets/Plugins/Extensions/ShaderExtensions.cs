using System.Collections.Generic;
using UnityEngine;

namespace BalsamicBits.Extensions
{
	public static class ShaderExtensions
	{
		public static List<string> GetPropertyNames(this Shader shader)
		{
			List<string> names = new List<string>(shader.GetPropertyCount());
			for (int i = 0; i < shader.GetPropertyCount(); i++)
			{
				names.Add(shader.GetPropertyName(i));
			}

			return names;
		}
	}
}