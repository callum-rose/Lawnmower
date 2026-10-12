using System;
using System.Linq;
using NUnit.Framework;

namespace Tests.EditMode
{
	public class NoOdinTests
	{
		[Test]
		public void NoSirenixAssembliesAreLoaded()
		{
			string[] sirenix = AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetName().Name)
				.Where(n => n.StartsWith("Sirenix", StringComparison.Ordinal))
				.ToArray();

			CollectionAssert.IsEmpty(sirenix);
		}
	}
}
