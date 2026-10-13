#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using Lawnmower.Services;
using NUnit.Framework;
using Settings;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	/// <summary>The services as the player gets them: bootstrapped before the first scene, with no scene edits.</summary>
	public class GameServicesPlayModeTests
	{
		private sealed class SoundRecorder : ISoundEffects
		{
			public readonly List<SoundEffect> Played = new List<SoundEffect>();
			public void Play(SoundEffect effect) => Played.Add(effect);
		}

		private MovementScheme _schemeBefore;

		[SetUp]
		public void SetUp()
		{
			_schemeBefore = MovementSchemeSetting.Current;
			GameServices.InitialiseForTests();
		}

		[TearDown]
		public void TearDown()
		{
			GameServices.SoundEffectsOverrideForTests = null;
			GameServices.Shutdown();
			MovementSchemeSetting.Current = _schemeBefore;
		}

		[Test]
		public void Bootstrap_CreatesOnePersistentHost_WithAnSfxAudioSource()
		{
			GameServicesHost[] hosts = Object.FindObjectsByType<GameServicesHost>(FindObjectsInactive.Include);

			Assert.AreEqual(1, hosts.Length);
			AudioSource source = hosts[0].GetComponent<AudioSource>();
			Assert.IsNotNull(source);
			Assert.AreEqual("SFX", source.outputAudioMixerGroup.name);
			Assert.AreEqual("DontDestroyOnLoad", hosts[0].gameObject.scene.name);
		}

		[UnityTest]
		public IEnumerator KitButtonClick_PlaysTheClickSound()
		{
			SoundRecorder sounds = new SoundRecorder();
			GameServices.SoundEffectsOverrideForTests = sounds;

			GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
			go.AddComponent<ButtonView>();
			yield return null;

			go.GetComponent<Button>().onClick.Invoke();
			Object.Destroy(go);

			CollectionAssert.AreEqual(new[] { SoundEffect.Click }, sounds.Played);
		}

		[UnityTest]
		public IEnumerator VolumeSliders_SetTheMixer()
		{
			yield return null;
			AudioMixer mixer = Resources.Load<GameServicesConfig>(GameServicesConfig.ResourcesPath).Mixer;

			GameServices.Settings.SoundsVolume = 0.5f;
			GameServices.Settings.MusicVolume = 0f;

			Assert.IsTrue(mixer.GetFloat("SfxVolume", out float sfx));
			Assert.IsTrue(mixer.GetFloat("MusicVolume", out float music));
			Assert.AreEqual(AudioVolume.ToDecibels(0.5f), sfx, 0.01f);
			Assert.AreEqual(AudioVolume.SilentDecibels, music, 0.01f);
		}

		[Test]
		public void GraphicsSetting_AppliesInPlayMode()
		{
			GameServices.Settings.Graphics = GraphicsQuality.Low;

			Assert.AreEqual(GraphicsSetting.Low, GraphicsSettingState.Current);
			Assert.AreEqual("Low", QualitySettings.names[QualitySettings.GetQualityLevel()]);

			GameServices.Settings.Graphics = GraphicsQuality.High;

			Assert.AreEqual(GraphicsSetting.High, GraphicsSettingState.Current);
			Assert.AreEqual("High", QualitySettings.names[QualitySettings.GetQualityLevel()]);
		}

		[Test]
		public void MovementSetting_SwitchesTheInputScheme()
		{
			GameServices.Settings.Movement = MovementScheme.Arrows;

			Assert.AreEqual(MovementScheme.Arrows, MovementSchemeSetting.Current);
		}
	}
}
#endif
