using System.Linq;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class PresenterArchitectureTests
	{
		[Test]
		public void PresentersAssembly_ReferencesOnlyTheModel()
		{
			string[] references = typeof(GamePresenter).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

			CollectionAssert.Contains(references, "Lawnmower.Model");
			Assert.That(references.Where(r => r.StartsWith("Unity") || r.StartsWith("Lawnmower.Presentation")
					|| r.StartsWith("Lawnmower.Infrastructure") || r.StartsWith("Assembly-CSharp")),
				Is.Empty, string.Join(", ", references));
		}
	}
}
