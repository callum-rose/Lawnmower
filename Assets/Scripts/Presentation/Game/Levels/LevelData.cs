using System;
using Core;
using Game.Core;
using Game.Levels.Serialization;
using Lawnmower.Model;
using UnityEngine;

namespace Game.Levels
{
	/// <summary>
	/// A level definition asset. The tiles are persisted once, as Unity-serialized <see cref="TileRecord"/>s
	/// (row-major, index = x + y * width). Play never mutates this asset: <see cref="CreateLevel"/>
	/// builds fresh runtime tiles each time.
	/// </summary>
	[CreateAssetMenu(fileName = "LevelData", menuName = SoNames.GameDir + "Level Data")]
	internal class LevelData : ScriptableObject
	{
		/// <summary>Level id in Guid "N" format. Keys player progress, so never change it once shipped.</summary>
		[SerializeField] private string guid;

		[SerializeField] private GridVector startPosition = new GridVector(0, 0);

		[SerializeField, Min(1)] private int width = 1;
		[SerializeField, Min(1)] private int depth = 1;

		[SerializeField] private TileRecord[] tiles = { new TileRecord() };

		[SerializeField, Tooltip("U/D/L/R moves, verified by LevelAssetSimulationTests")]
		private string solution;

		[SerializeField, Min(0), Tooltip("Shortest solution length, written by Bunny Heaven/Recompute Par; 0 = unknown")]
		private int par;

		[NonSerialized] private LevelDefinition _definition;

		public Guid Id => Guid.TryParse(guid, out Guid g) ? g : Guid.Empty;

		public int Width => width;
		public int Depth => depth;
		public GridVector StartPosition => startPosition;

		/// <summary>A known solution in <see cref="MoveNotation"/> (U/D/L/R); empty if none is recorded.</summary>
		public string Solution => solution;

		/// <summary>The solver's shortest solution length; 0 if unknown (not yet computed, or too big to solve).</summary>
		public int Par => par;

		#region Unity

		private void OnValidate()
		{
			_definition = null;
		}

#if UNITY_EDITOR
		private void Reset()
		{
			guid = Guid.NewGuid().ToString("N");
		}
#endif

		#endregion

		#region API

		public LevelDefinition ToDefinition()
		{
			return _definition ??= TileRecordCodec.Decode(Id, width, depth, startPosition, tiles);
		}

		/// <summary>A fresh runtime level (new tile objects every call).</summary>
		public Level CreateLevel()
		{
			return Level.FromDefinition(ToDefinition());
		}

		/// <summary>Overwrites this asset's layout. The guid is never changed.</summary>
		internal void SetContents(LevelDefinition definition)
		{
			tiles = TileRecordCodec.Encode(definition);
			width = definition.Width;
			depth = definition.Depth;
			startPosition = definition.StartPosition;
			_definition = null;
		}

		#endregion
	}
}
