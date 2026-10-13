using System;
using System.Linq;
using Game.Haptics;
using Game.Levels;
using Lawnmower.Presenters;
using Lawnmower.Services;
using NUnit.Framework;
using Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tests.EditMode
{
	public class GameServicesTests
	{
		private MovementScheme _schemeBefore;
		private bool _hapticsBefore;

		[SetUp]
		public void SetUp()
		{
			_schemeBefore = MovementSchemeSetting.Current;
			_hapticsBefore = HapticFeedback.Enabled;
		}

		[TearDown]
		public void TearDown()
		{
			GameServices.Shutdown();
			MovementSchemeSetting.Current = _schemeBefore;
			HapticFeedback.Enabled = _hapticsBefore;
		}

		private static GameServicesConfig LoadConfig() => Resources.Load<GameServicesConfig>(GameServicesConfig.ResourcesPath);

		[Test]
		public void Config_IsInResources_WithEveryAsset()
		{
			GameServicesConfig config = LoadConfig();

			Assert.IsNotNull(config);
			Assert.IsNotNull(config.Levels);
			Assert.IsNotNull(config.Mixer);
			Assert.AreEqual("SFX", config.SoundsGroup.name);
			Assert.IsTrue(config.Mixer.GetFloat(config.MusicVolumeParameter, out _), config.MusicVolumeParameter);
			Assert.IsTrue(config.Mixer.GetFloat(config.SoundsVolumeParameter, out _), config.SoundsVolumeParameter);

			foreach (SoundEffect effect in Enum.GetValues(typeof(SoundEffect)))
			{
				Assert.IsNotNull(config.GetClip(effect), effect.ToString());
			}

			CollectionAssert.Contains(QualitySettings.names, config.LowQualityLevel);
			CollectionAssert.Contains(QualitySettings.names, config.HighQualityLevel);
		}

		[Test]
		public void Progression_FollowsTheLevelList()
		{
			GameServices.InitialiseForTests();
			LevelDataManager levels = LoadConfig().Levels;

			CollectionAssert.AreEqual(levels.Levels.Select(l => l.Id), GameServices.Progression.Levels);
			Assert.AreEqual(10, GameServices.Progression.LevelCount);
		}

		[Test]
		public void LevelDataManager_LocksFromProgression()
		{
			GameServices.InitialiseForTests();
			LevelDataManager levels = LoadConfig().Levels;

			Assert.IsFalse(levels.GetLevel(0).Locked);
			Assert.IsTrue(levels.GetLevel(1).Locked);

			GameServices.Progression.MarkCompleted(levels.Levels[0].Id, 7, true);

			Assert.IsFalse(levels.GetLevel(1).Locked, "B5: completing level 1 opens level 2");
			Assert.IsTrue(levels.GetLevel(2).Locked);

			GameServices.Progression.UnlockAll();

			Assert.IsTrue(levels.GetAllLevels().All(l => !l.Locked), "B6: unlock-all opens everything");
		}

		[Test]
		public void InitialiseForTests_UsesTheGivenStores()
		{
			InMemoryProgressStore progress = new InMemoryProgressStore();
			InMemorySettingsStore settings = new InMemorySettingsStore(new SettingsData { movement = MovementScheme.Arrows });

			GameServices.InitialiseForTests(progress, settings);
			GameServices.Progression.UnlockAll();

			Assert.AreEqual(1, progress.SaveCount);
			Assert.AreEqual(MovementScheme.Arrows, GameServices.Settings.Movement);
			Assert.AreEqual(MovementScheme.Arrows, MovementSchemeSetting.Current);
		}

		[Test]
		public void HapticsToggle_DrivesTheHapticPlugin()
		{
			GameServices.InitialiseForTests();

			GameServices.Settings.Haptics = false;
			Assert.IsFalse(HapticFeedback.Enabled);

			GameServices.Settings.Haptics = true;
			Assert.IsTrue(HapticFeedback.Enabled);
		}

		[Test]
		public void SavedHapticsOff_IsAppliedAtStartup()
		{
			GameServices.InitialiseForTests(settingsStore: new InMemorySettingsStore(new SettingsData { haptics = false }));

			Assert.IsFalse(HapticFeedback.Enabled);
		}

		[Test]
		public void SoundEffects_AreSilentOutsidePlayMode()
		{
			Assert.DoesNotThrow(() => GameServices.SoundEffects.Play(SoundEffect.Click));
		}
	}

	public class GraphicsQualityApplierTests
	{
		[Test]
		public void LowAndHigh_SetTheQualityLevelRenderScaleAndShaderKeywords_ThenRestore()
		{
			UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
			float scaleBefore = urp.renderScale;
			int levelBefore = QualitySettings.GetQualityLevel();
			GraphicsQualityApplier applier = new GraphicsQualityApplier("Low", "High", 0.6f, 1f);

			try
			{
				applier.Apply(GraphicsQuality.Low);

				Assert.AreEqual("Low", QualitySettings.names[QualitySettings.GetQualityLevel()]);
				Assert.AreEqual(0.6f, urp.renderScale, 1e-5);
				Assert.AreEqual(GraphicsSetting.Low, GraphicsSettingState.Current);

				applier.Apply(GraphicsQuality.High);

				Assert.AreEqual("High", QualitySettings.names[QualitySettings.GetQualityLevel()]);
				Assert.AreEqual(1f, urp.renderScale, 1e-5);
				Assert.AreEqual(GraphicsSetting.High, GraphicsSettingState.Current);
			}
			finally
			{
				applier.Restore();
			}

			Assert.AreEqual(scaleBefore, urp.renderScale);
			Assert.AreEqual(levelBefore, QualitySettings.GetQualityLevel());
			Assert.AreEqual(GraphicsSettingState.Default, GraphicsSettingState.Current);
		}

		[Test]
		public void GrassShader_HasTheQualityKeywords_ThatShaderQualityManagerSets()
		{
			Shader grass = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Game/Tiles/Grass/Grass.shader");
			ShaderQualityManager manager = AssetDatabase.LoadAssetAtPath<ShaderQualityManager>("Assets/ScriptableObjects/Settings/ShaderQualityManager.asset");
			SerializedObject so = new SerializedObject(manager);

			string[] keywords = grass.keywordSpace.keywordNames;

			CollectionAssert.Contains(keywords, so.FindProperty("graphicsSettingsLowShaderKeyword").stringValue);
			CollectionAssert.Contains(keywords, so.FindProperty("graphicSettingsHighShaderKeyword").stringValue);
		}

		[Test]
		public void ShaderQualityManager_FollowsTheGraphicsSetting()
		{
			ShaderQualityManager manager = AssetDatabase.LoadAssetAtPath<ShaderQualityManager>("Assets/ScriptableObjects/Settings/ShaderQualityManager.asset");
			SerializedObject so = new SerializedObject(manager);
			string low = so.FindProperty("graphicsSettingsLowShaderKeyword").stringValue;
			string high = so.FindProperty("graphicSettingsHighShaderKeyword").stringValue;

			try
			{
				GraphicsSettingState.Current = GraphicsSetting.Low;
				Assert.IsTrue(Shader.IsKeywordEnabled(low));
				Assert.IsFalse(Shader.IsKeywordEnabled(high));

				GraphicsSettingState.Current = GraphicsSetting.High;
				Assert.IsTrue(Shader.IsKeywordEnabled(high));
				Assert.IsFalse(Shader.IsKeywordEnabled(low));
			}
			finally
			{
				GraphicsSettingState.Current = GraphicsSettingState.Default;
			}
		}
	}

	public class PlayerPrefsStoreTests
	{
		private const string ProgressKey = "BunnyHeaven.Tests.Progress";
		private const string SettingsKey = "BunnyHeaven.Tests.Settings";

		[TearDown]
		public void TearDown()
		{
			PlayerPrefs.DeleteKey(ProgressKey);
			PlayerPrefs.DeleteKey(SettingsKey);
		}

		[Test]
		public void Progress_RoundTripsThroughJson()
		{
			Guid level = Guid.NewGuid();
			PlayerPrefsProgressStore store = new PlayerPrefsProgressStore(ProgressKey);
			Assert.IsNull(store.Load());

			Progression progression = new Progression(new[] { level }, store);
			progression.MarkCompleted(level, 9, true);
			progression.UnlockAll();

			Progression reloaded = new Progression(new[] { level }, new PlayerPrefsProgressStore(ProgressKey));

			Assert.AreEqual(9, reloaded.Best(level));
			Assert.IsTrue(reloaded.IsPerfect(level));
			Assert.IsTrue(reloaded.IsAllUnlocked);
			StringAssert.Contains(level.ToString("N"), PlayerPrefs.GetString(ProgressKey));
		}

		[Test]
		public void Settings_RoundTripThroughJson()
		{
			MovementScheme schemeBefore = MovementSchemeSetting.Current;

			try
			{
				GameSettings settings = new GameSettings(new PlayerPrefsSettingsStore(SettingsKey))
				{
					Graphics = GraphicsQuality.Low,
					Movement = MovementScheme.Arrows,
					Haptics = false,
					MusicVolume = 0.2f,
					SoundsVolume = 0.7f
				};

				SettingsData loaded = new PlayerPrefsSettingsStore(SettingsKey).Load();

				Assert.AreEqual(GraphicsQuality.Low, loaded.graphics);
				Assert.AreEqual(MovementScheme.Arrows, loaded.movement);
				Assert.IsFalse(loaded.haptics);
				Assert.AreEqual(0.2f, loaded.musicVolume, 1e-6);
				Assert.AreEqual(0.7f, loaded.soundsVolume, 1e-6);
			}
			finally
			{
				MovementSchemeSetting.Current = schemeBefore;
			}
		}

		[Test]
		public void CorruptData_LoadsAsNothingSaved()
		{
			PlayerPrefs.SetString(ProgressKey, "{not json");

			UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Ignoring unreadable saved data"));

			Assert.IsNull(new PlayerPrefsProgressStore(ProgressKey).Load());
		}
	}
}
