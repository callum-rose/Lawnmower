using System;
using UnityEngine;
using Newtonsoft.Json;
using Sirenix.OdinInspector;

namespace Game.Tiles
{
    [Serializable]
    public struct TileData : ISerializationCallbackReceiver
    {
        [SerializeField, EnumToggleButtons] private TileType type;
        [SerializeField] private string dataStr;

        public TileType Type
        {
            get => type;
            set => type = value;
        }

        public BaseTileSetupData Data { get; set; }

        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public void UpdateJsonData()
        {
            dataStr = JsonConvert.SerializeObject(Data, jsonSettings);
        }

        public void OnAfterDeserialize()
        {
            Data = JsonConvert.DeserializeObject(dataStr, jsonSettings) as BaseTileSetupData;
        }

        public void OnBeforeSerialize()
        {
            dataStr = JsonConvert.SerializeObject(Data, jsonSettings);
        }

        public override string ToString()
        {
            switch (Type)
            {
                case TileType.Empty:
                case TileType.Stone:
                case TileType.Water:
                case TileType.Wood:
                    return Type.ToString();

                case TileType.Grass:
                    return $"{Type.ToString()} {(Data as GrassTileSetupData).grassHeight}";

                default:
                    throw new ArgumentException();
            }
        }

        public static class Factory
        {
            public readonly static TileData Default = new TileData
            {
                Type = TileType.Empty,
                Data = null
            };

            public static TileData Create(TileType type, BaseTileSetupData data)
            {
                return new TileData
                {
                    Type = type,
                    Data = data
                };
            }
        }
    }
}
