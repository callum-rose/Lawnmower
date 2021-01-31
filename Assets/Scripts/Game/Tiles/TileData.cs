using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[Serializable]
	public struct TileData : ISerializationCallbackReceiver
	{
		[SerializeField, OnValueChanged(nameof(OnValueChanged))]
		private TileType type;
		
		private bool IsGrass => type == TileType.Grass;
		[SerializeField, ShowIf(nameof(IsGrass)), InlineProperty, HideLabel, OnValueChanged(nameof(OnValueChanged))] private GrassTileSetupData grassTileSetupData;

		[SerializeField, HideInInspector] private string dataStr;

		public TileType Type
		{
			get => type;
			set => type = value;
		}

		public object Data { get; set; }

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
			Data = JsonConvert.DeserializeObject(dataStr, jsonSettings);
		}

		public void OnBeforeSerialize()
		{
			UpdateJsonData();
		}

		private void OnValueChanged()
		{
			switch (type)
			{
				case TileType.Grass:
					Data = grassTileSetupData;
					break;
				
				case TileType.Empty:
				case TileType.Stone:
				case TileType.Water:
				case TileType.Wood:
					Data = null;
					break;
				
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			UpdateJsonData();
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
					return $"{Type.ToString()} {((GrassTileSetupData)Data).grassHeight}";

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

			public static TileData Create(TileType type, object data)
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