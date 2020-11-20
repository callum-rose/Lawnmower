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

        [ShowInInspector] private string _key;

        [ShowInInspector]
        private string Value
        {
            get
            {
                if (TryLoad(out T value))
                {
                    return value.ToString();
                }
                else
                {
                    return "Undefined";
                }
            }
        }

        public PlayerPrefsItem(string key)
        {
            _key = key;
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

        public bool TryLoad(out T value)
        {
            if (!PlayerPrefs.HasKey(_key))
            {
                value = default;
                return false;
            }

            if (typeof(T) == typeof(int))
            {
                int v = PlayerPrefs.GetInt(_key);
                value = (T)Convert.ChangeType(v, typeof(T));
            }
            else if (typeof(T) == typeof(float))
            {
                float v = PlayerPrefs.GetFloat(_key);
                value = (T)Convert.ChangeType(v, typeof(T));
            }
            else if (typeof(T) == typeof(string))
            {
                string v = PlayerPrefs.GetString(_key);
                value = (T)Convert.ChangeType(v, typeof(T));
            }
            else
            {
                string json = PlayerPrefs.GetString(_key);
                value = JsonConvert.DeserializeObject<T>(json);
            }

            return true;
        }
    }
}
