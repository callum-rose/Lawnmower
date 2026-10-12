using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Haptics.Tests
{
    public class AudioAssetTests
    {
        private const string MixerPath = "Assets/Audio/BunnyHeaven/BunnyHeavenMixer.mixer";

        [TestCase("MasterVolume")]
        [TestCase("MusicVolume")]
        [TestCase("SfxVolume")]
        public void MixerExposesVolumeParameter(string parameter)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.IsNotNull(mixer);
            Assert.IsTrue(mixer.GetFloat(parameter, out _), parameter);
        }

        [TestCase("Master")]
        [TestCase("Music")]
        [TestCase("SFX")]
        public void MixerHasGroup(string group)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer.FindMatchingGroups(group), Has.Some.Matches<AudioMixerGroup>(g => g.name == group));
        }

        [TestCase("SfxMove")]
        [TestCase("SfxBump")]
        [TestCase("SfxClick")]
        [TestCase("SfxWin")]
        [TestCase("SfxFail")]
        public void SoundEffectImports(string clip)
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/BunnyHeaven/Sfx/{clip}.ogg"));
        }
    }
}
