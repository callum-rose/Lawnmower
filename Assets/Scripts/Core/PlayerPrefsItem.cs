using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Core
{
    internal class PlayerPrefsItem<T> : IPersistentDataItem<T>
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        [ShowInInspector] private readonly string _key;
        [ShowInInspector] private readonly T _defaultValue;

        [ShowInInspector] private string Value => Load().ToString();

        public PlayerPrefsItem(string key, T defaultValue)
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public void Save(T value)
        {
            if (typeof(T) == typeof(int))
            {
                PlayerPrefs.SetInt(_key, Convert.ToInt32(value));
            }
            else if (typeof(T) == typeof(float))
            {
                PlayerPrefs.SetFloat(_key, Convert.ToSingle(value));
            }
            else if (typeof(T) == typeof(string))
            {
                PlayerPrefs.SetString(_key, Convert.ToString(value));
            }
            else
            {
                string json = JsonConvert.SerializeObject(value, Settings);
                PlayerPrefs.SetString(_key, json);
            }
        }

        public T Load()
        {
            if (!PlayerPrefs.HasKey(_key))
            {
                return _defaultValue;
            }

            if (typeof(T) == typeof(int))
            {
                int v = PlayerPrefs.GetInt(_key);
                return (T)Convert.ChangeType(v, typeof(T));
            }
            else if (typeof(T) == typeof(float))
            {
                float v = PlayerPrefs.GetFloat(_key);
                return (T)Convert.ChangeType(v, typeof(T));
            }
            else if (typeof(T) == typeof(string))
            {
                string v = PlayerPrefs.GetString(_key);
                return (T)Convert.ChangeType(v, typeof(T));
            }
            else
            {
                string json = PlayerPrefs.GetString(_key);
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}
