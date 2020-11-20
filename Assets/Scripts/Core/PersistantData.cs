using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace Core
{
    internal static class PersistantData
    {
        [ShowInInspector]
        public static MowerModule Mower { get; } = new MowerModule();

        [ShowInInspector]
        public static LevelModule Level { get; } = new LevelModule();

        [Serializable]
        public class MowerModule
        {
            [ShowInInspector] public IPersistentDataItem<Guid> CurrentId { get; } = new PlayerPrefsItem<Guid>("mowerId");
        }

        [Serializable]
        public class LevelModule
        {
            [ShowInInspector] public IPersistentDataItem<int> LevelsCompleted { get; } = new PlayerPrefsItem<int>("levelsCompleted");

            private Dictionary<Guid, IPersistentDataItem<LevelMetaData>> _levelMetaData = new Dictionary<Guid, IPersistentDataItem<LevelMetaData>>();

            public void SaveLevelMetaData(Guid id, LevelMetaData data)
            {
                if (!_levelMetaData.TryGetValue(id, out IPersistentDataItem<LevelMetaData> saveItem))
                {
                    saveItem = new TextFileItem<LevelMetaData>("levelmetadata_" + id);
                }

                saveItem.Save(data);
            }

            public bool TryGetLevelMetaData(Guid id, out LevelMetaData data)
            {
                if (!_levelMetaData.TryGetValue(id, out IPersistentDataItem<LevelMetaData> saveItem))
                {
                    saveItem = new TextFileItem<LevelMetaData>("levelmetadata_" + id);
                }

                return saveItem.TryLoad(out data);
            }
        }
    }

    [Serializable]
    public class LevelMetaData
    {
        public float time;
    }
}
