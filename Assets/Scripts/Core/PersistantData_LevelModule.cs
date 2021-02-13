using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;

namespace Core
{
	internal static partial class PersistantData
	{
		public static class LevelModule
		{
			public static readonly IPersistentDataItem<bool> TutorialCompleted = new PlayerPrefsItem<bool>("tutorialCompleted", false);		
			public static readonly IPersistentDataItem<int> LevelsCompleted = new PlayerPrefsItem<int>("levelsCompleted", 0);

			private const string Folder = "level_meta_data";
			
			private static Dictionary<Guid, IPersistentDataItem<MetaData>> _levelMetaData = new Dictionary<Guid, IPersistentDataItem<MetaData>>();

			static LevelModule()
			{
				InitAllMetaDataFiles();
			}

			private static void InitAllMetaDataFiles()
			{
				string directory = TextFileItem.GetDirectoryPath(Folder);

				if (!Directory.Exists(directory))
				{
					return;
				}

				string[] filePaths = Directory.GetFiles(directory);

				if (filePaths.Length == 0)
				{
					return;
				}

				string[] keys = filePaths.Select(TextFileItem.GetKeyFromFilePath).ToArray();
				string[] idStrs = keys.Select(i => i.Split('_')[1]).ToArray();
				Guid[] ids = idStrs.Select(Guid.Parse).ToArray();
				foreach (Guid id in ids)
				{
					CreateSaveItem(id);
				}
			}

			#region API

			public static void SaveLevelMetaData(Guid id, MetaData data)
			{
				IPersistentDataItem<MetaData> saveItem = GetPersistentDataItem(id);
				saveItem.Save(data);
			}

			public static MetaData GetLevelMetaData(Guid id)
			{
				IPersistentDataItem<MetaData> saveItem = GetPersistentDataItem(id);
				return saveItem.Load();
			}

			#endregion

			#region Methods

			private static IPersistentDataItem<MetaData> GetPersistentDataItem(Guid id)
			{
				if (_levelMetaData.TryGetValue(id, out IPersistentDataItem<MetaData> saveItem))
				{
					return saveItem;
				}

				saveItem = CreateSaveItem(id);

				return saveItem;
			}

			private static IPersistentDataItem<MetaData> CreateSaveItem(Guid id)
			{
				IPersistentDataItem<MetaData> saveItem = new TextFileItem<MetaData>(Folder, "metadata_" + id, null);
				_levelMetaData.Add(id, saveItem);
				return saveItem;
			}

			#endregion

			#region Classes

			[Serializable]
			public class MetaData
			{
				public float timeTaken;
				public int undoCount;
			}

			#endregion
		}
	}
}