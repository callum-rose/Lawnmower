using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace Core
{
    internal class TextFileItem<T> : IPersistentDataItem<T>
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        private readonly string _directory;
        private readonly string _fullPath;

        public TextFileItem(string key)
        {
            _directory = Path.Combine(Application.persistentDataPath, "savedata");
            _fullPath = Path.Combine(_directory, key + ".txt");
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

        public bool TryLoad(out T value)
        {
            Debug.Log(_fullPath);

            if (!File.Exists(_fullPath))
            {
                value = default;
                return false;
            }

            string jsonStr = File.ReadAllText(_fullPath);
            value = JsonConvert.DeserializeObject<T>(jsonStr, Settings);
            return true;
        }
    }
}
