using System.Collections.Generic;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class GameSettingsTests
	{
		private MovementScheme _schemeBefore;
		private InMemorySettingsStore _store;

		[SetUp]
		public void SetUp()
		{
			_schemeBefore = MovementSchemeSetting.Current;
			_store = new InMemorySettingsStore();
		}

		[TearDown]
		public void TearDown()
		{
			MovementSchemeSetting.Current = _schemeBefore;
		}

		[Test]
		public void NothingSaved_GivesTheDefaults_WithoutSaving()
		{
			GameSettings settings = new GameSettings(_store);

			Assert.AreEqual(GraphicsQuality.High, settings.Graphics);
			Assert.AreEqual(MovementScheme.Swipe, settings.Movement);
			Assert.IsTrue(settings.Haptics);
			Assert.AreEqual(1f, settings.MusicVolume);
			Assert.AreEqual(1f, settings.SoundsVolume);
			Assert.AreEqual(0, _store.SaveCount);
		}

		[Test]
		public void EverySetting_SurvivesAReload()
		{
			GameSettings settings = new GameSettings(_store)
			{
				Graphics = GraphicsQuality.Low,
				Movement = MovementScheme.Arrows,
				Haptics = false,
				MusicVolume = 0.25f,
				SoundsVolume = 0.5f
			};

			GameSettings reloaded = new GameSettings(_store);

			Assert.AreEqual(GraphicsQuality.Low, reloaded.Graphics);
			Assert.AreEqual(MovementScheme.Arrows, reloaded.Movement);
			Assert.IsFalse(reloaded.Haptics);
			Assert.AreEqual(0.25f, reloaded.MusicVolume);
			Assert.AreEqual(0.5f, reloaded.SoundsVolume);
			Assert.AreEqual(5, _store.SaveCount);
		}

		[Test]
		public void Setters_RaiseTheirEventAndChanged()
		{
			GameSettings settings = new GameSettings(_store);
			List<string> log = new List<string>();
			settings.GraphicsChanged += v => log.Add($"graphics {v}");
			settings.MovementChanged += v => log.Add($"movement {v}");
			settings.HapticsChanged += v => log.Add($"haptics {v}");
			settings.MusicVolumeChanged += v => log.Add($"music {v}");
			settings.SoundsVolumeChanged += v => log.Add($"sounds {v}");
			settings.Changed += () => log.Add("changed");

			settings.Graphics = GraphicsQuality.Low;
			settings.Movement = MovementScheme.Arrows;
			settings.Haptics = false;
			settings.MusicVolume = 0.5f;
			settings.SoundsVolume = 0f;

			CollectionAssert.AreEqual(new[]
			{
				"graphics Low", "changed", "movement Arrows", "changed", "haptics False", "changed",
				"music 0.5", "changed", "sounds 0", "changed"
			}, log);
		}

		[Test]
		public void SettingTheSameValue_DoesNothing()
		{
			GameSettings settings = new GameSettings(_store);
			int changed = 0;
			settings.Changed += () => changed++;

			settings.Graphics = GraphicsQuality.High;
			settings.Movement = MovementScheme.Swipe;
			settings.Haptics = true;
			settings.MusicVolume = 1f;
			settings.SoundsVolume = 1f;

			Assert.AreEqual(0, changed);
			Assert.AreEqual(0, _store.SaveCount);
		}

		[TestCase(-1f, 0f)]
		[TestCase(2f, 1f)]
		[TestCase(float.NaN, 1f)]
		public void Volumes_AreClamped(float value, float expected)
		{
			GameSettings settings = new GameSettings(_store) { MusicVolume = 0.5f, SoundsVolume = 0.5f };

			settings.MusicVolume = value;
			settings.SoundsVolume = value;

			Assert.AreEqual(expected, settings.MusicVolume);
			Assert.AreEqual(expected, settings.SoundsVolume);
		}

		[Test]
		public void Movement_DrivesTheInputSchemeSetting()
		{
			MovementSchemeSetting.Current = MovementScheme.Swipe;
			GameSettings settings = new GameSettings(_store);

			settings.Movement = MovementScheme.Arrows;

			Assert.AreEqual(MovementScheme.Arrows, MovementSchemeSetting.Current);
		}

		[Test]
		public void Loading_AppliesTheSavedMovementScheme()
		{
			MovementSchemeSetting.Current = MovementScheme.Swipe;

			new GameSettings(new InMemorySettingsStore(new SettingsData { movement = MovementScheme.Arrows }));

			Assert.AreEqual(MovementScheme.Arrows, MovementSchemeSetting.Current);
		}

		[Test]
		public void Loading_RepairsBadValues()
		{
			SettingsData bad = new SettingsData
			{
				graphics = (GraphicsQuality)7,
				movement = (MovementScheme)9,
				musicVolume = 5f,
				soundsVolume = -3f
			};

			GameSettings settings = new GameSettings(new InMemorySettingsStore(bad));

			Assert.AreEqual(GraphicsQuality.High, settings.Graphics);
			Assert.AreEqual(MovementScheme.Swipe, settings.Movement);
			Assert.AreEqual(1f, settings.MusicVolume);
			Assert.AreEqual(0f, settings.SoundsVolume);
		}
	}
}
