using System;
using Game.Core;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Tiles
{
	[Serializable]
	internal class GrassTile : Tile
	{
		public const int MaxGrassHeight = 3;
		public const int PerfectGrassHeight = 1;

		public override bool IsComplete => _internalGrassHeight == PerfectGrassHeight;
		public override bool IsRuined => _internalGrassHeight <= 0;

		public IListenableProperty<int> GrassHeight => _internalGrassHeight;

		private readonly EventProperty<int> _internalGrassHeight;

		public GrassTile()
		{
			_internalGrassHeight = new EventProperty<int>(i => Mathf.Clamp(i, 0, MaxGrassHeight));
		}
		
		public GrassTile(int grassHeight) : this()
		{
			_internalGrassHeight.SetValue(grassHeight, false);
		}

		#region API

		public override bool IsTraversable(bool editMode) => !editMode || _internalGrassHeight < MaxGrassHeight;

		public override void TraverseOnto(GridVector fromDirection, Xor inverted)
		{
			int newHeight;
			if (!inverted)
			{
				newHeight = _internalGrassHeight.RawValue - 1;
			}
			else
			{
				newHeight = _internalGrassHeight.RawValue + 1;

				if (_internalGrassHeight > MaxGrassHeight)
				{
					throw new InvalidOperationException("Grass height too high");
				}
			}
			
			_internalGrassHeight.SetValue(newHeight, inverted);

			base.TraverseOnto(fromDirection, inverted);
		}

		public override void TraverseAway(GridVector toDirection, Xor inverted) =>
			base.TraverseAway(toDirection, inverted);

		#endregion

		#region Methods

		public override string ToString()
		{
			return $"{GetType()}: {_internalGrassHeight.Value}";
		}

		#endregion
	}
}