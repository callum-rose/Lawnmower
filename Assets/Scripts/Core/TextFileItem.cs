using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace Core
{
	internal abstract class TextFileItem
	{
		public static string GetKeyFromFilePath(string path)
		{
			return Path.GetFileNameWithoutExtension(path);
		}

		public static string GetDirectoryPath(string folder)
		{
			return Path.Combine(Application.persistentDataPath, "save_data", folder);
		}
		
		public static string GetFilePath(string directory, string key)
		{
			return Path.Combine(directory, key + ".txt");
		}
	}
	
	internal class TextFileItem<T> : TextFileItem, IPersistentDataItem<T>
	{
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		
		private readonly T _defaultValue;

		private readonly string _directory;
		private readonly string _fullPath;
		
		public TextFileItem(string folder, string key, T defaultValue)
		{
			_defaultValue = defaultValue;

			_directory = GetDirectoryPath(folder);
			_fullPath = GetFilePath(_directory, key);
		}

		public void Save(T value)
		{
			string jsonStr = JsonConvert.SerializeObject(value, Settings);

			if (!Directory.Exists(_directory))
			{
				Directory.CreateDirectory(_directory);
			}

			if (!File.Exists(_fullPath))
			{
				File.Create(_fullPath);
			}

			File.WriteAllText(_fullPath, jsonStr);
		}

		public T Load()
		{
			Debug.Log(_fullPath);

			if (!File.Exists(_fullPath))
			{
				return _defaultValue;
			}

			string jsonStr = File.ReadAllText(_fullPath);
			return JsonConvert.DeserializeObject<T>(jsonStr, Settings);
		}
	}
}