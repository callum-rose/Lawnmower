using System;
using Game.Core;
using Game.UndoSystem;
using Lawnmower.Model;

namespace Game.Tiles
{
	public sealed class GrassTile : Tile, IReadonlyGrassTile
	{
		public const int MaxGrassHeight = 3;
		public const int PerfectGrassHeight = 1;

		public override TileKind Kind => TileKind.Grass;
		public override bool IsTraversable => true;
		public override bool IsComplete => _internalGrassHeight.Value == PerfectGrassHeight;
		public override bool IsRuined => _internalGrassHeight.Value <= 0;

		public IListenableProperty<int> GrassHeight => _internalGrassHeight;

		private readonly EventProperty<int> _internalGrassHeight;

		public GrassTile(int grassHeight)
		{
			// Out-of-range heights used to be clamped only on read, so a height-5 tile needed three
			// mows to show a change and its first undo threw (B10).
			if (grassHeight < 0 || grassHeight > MaxGrassHeight)
			{
				throw new ArgumentOutOfRangeException(nameof(grassHeight), grassHeight,
					$"Grass height must be 0-{MaxGrassHeight}");
			}

			_internalGrassHeight = new EventProperty<int>(grassHeight, i => Math.Clamp(i, 0, MaxGrassHeight));
		}

		public override TileSpec ToSpec() => TileSpec.Grass(_internalGrassHeight.Value);

		internal override void TraverseOnto(GridVector fromDirection, Xor inverted)
		{
			int newHeight;
			if (!inverted)
			{
				newHeight = _internalGrassHeight.RawValue - 1;
			}
			else
			{
				newHeight = _internalGrassHeight.RawValue + 1;

				// Check the new raw (unclamped) value: the clamped getter can never exceed the max.
				if (newHeight > MaxGrassHeight)
				{
					throw new InvalidOperationException($"Grass height too high: {newHeight} > {MaxGrassHeight}");
				}
			}

			_internalGrassHeight.SetValue(newHeight, inverted);

			base.TraverseOnto(fromDirection, inverted);
		}
	}
}
