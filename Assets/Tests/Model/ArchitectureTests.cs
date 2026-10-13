using System.Linq;
using System.Reflection;
using Game.Core;
using Game.Tiles;
using NUnit.Framework;

namespace Lawnmower.Model.Tests
{
	public class ArchitectureTests
	{
		private static readonly string[] Forbidden =
			{ "UnityEngine", "UnityEditor", "Newtonsoft", "DOTween", "Sirenix", "Lawnmower.Presentation", "Lawnmower.Infrastructure", "Lawnmower.Presenters", "Assembly-CSharp" };

		[Test]
		public void ModelAssembly_ReferencesNoEngineOrGameAssemblies()
		{
			string[] references = typeof(LevelSimulation).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
			string[] bad = references.Where(r => Forbidden.Any(r.StartsWith)).ToArray();

			CollectionAssert.IsEmpty(bad, "Lawnmower.Model references: " + string.Join(", ", references));
		}

		[Test]
		public void ModelTestAssembly_ReferencesNoEngineOrGameAssemblies()
		{
			string[] references = typeof(ArchitectureTests).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
			string[] bad = references.Where(r => Forbidden.Any(r.StartsWith)).ToArray();

			CollectionAssert.IsEmpty(bad, "Lawnmower.Model.Tests references: " + string.Join(", ", references));
		}

		[TestCase(nameof(Tile.TraverseOnto))]
		[TestCase(nameof(Tile.TraverseAway))]
		[TestCase(nameof(Tile.BumpInto))]
		public void TileMutators_AreNotPublic(string name)
		{
			MethodInfo method = typeof(Tile).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.IsNotNull(method, name);
			Assert.IsFalse(method.IsPublic, name);
			Assert.IsFalse(method.IsFamily || method.IsFamilyOrAssembly, $"{name} must not be overridable outside the model");
		}

		[Test]
		public void EventPropertySetters_AreNotPublic()
		{
			foreach (string name in new[] { "SetValue", "SetValueSilently" })
			{
				MethodInfo method = typeof(EventProperty<int>).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				Assert.IsNotNull(method, name);
				Assert.IsFalse(method.IsPublic, name);
			}
		}

		[Test]
		public void LevelSimulation_StatusHasNoPublicSetter()
		{
			PropertyInfo status = typeof(LevelSimulation).GetProperty(nameof(LevelSimulation.Status));
			Assert.IsNull(status.GetSetMethod(false));
		}
	}
}
