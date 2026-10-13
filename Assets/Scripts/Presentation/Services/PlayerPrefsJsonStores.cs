using System;
using Lawnmower.Presenters;
using UnityEngine;

namespace Lawnmower.Services
{
	/// <summary>One JSON document in one PlayerPrefs key. A corrupt document loads as "nothing saved" (with a warning).</summary>
	internal abstract class PlayerPrefsJsonStore<T> where T : class
	{
		private readonly string _key;

		protected PlayerPrefsJsonStore(string key)
		{
			_key = key;
		}

		public T Load()
		{
			if (!PlayerPrefs.HasKey(_key))
			{
				return null;
			}

			try
			{
				return JsonUtility.FromJson<T>(PlayerPrefs.GetString(_key));
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Ignoring unreadable saved data in PlayerPrefs '{_key}': {e.Message}");
				return null;
			}
		}

		public void Save(T data)
		{
			PlayerPrefs.SetString(_key, JsonUtility.ToJson(data));
			PlayerPrefs.Save();
		}

		public void Delete()
		{
			PlayerPrefs.DeleteKey(_key);
			PlayerPrefs.Save();
		}
	}

	internal sealed class PlayerPrefsProgressStore : PlayerPrefsJsonStore<ProgressData>, IProgressStore
	{
		public const string Key = "BunnyHeaven.Progress";

		public PlayerPrefsProgressStore(string key = Key) : base(key) { }
	}

	internal sealed class PlayerPrefsSettingsStore : PlayerPrefsJsonStore<SettingsData>, ISettingsStore
	{
		public const string Key = "BunnyHeaven.Settings";

		public PlayerPrefsSettingsStore(string key = Key) : base(key) { }
	}
}
