using NUnit.Framework;

namespace Game.Haptics.Tests
{
    public class HapticFeedbackTests
    {
        [TearDown]
        public void TearDown() => HapticFeedback.Enabled = true;

        [Test]
        public void EnabledByDefault() => Assert.IsTrue(HapticFeedback.Enabled);

        [TestCase(true)]
        [TestCase(false)]
        public void CallsAreNoOpsInTheEditor(bool enabled)
        {
            HapticFeedback.Enabled = enabled;
            Assert.DoesNotThrow(HapticFeedback.LightTap);
            Assert.DoesNotThrow(HapticFeedback.Success);
            Assert.DoesNotThrow(HapticFeedback.Error);
        }
    }
}
