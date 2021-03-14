using System.Linq;
using BalsamicBits.Extensions;
using Game.Core;
using Game.Levels;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Tiles
{
	internal class WoodTileObject : BaseTileObject
	{
		[SerializeField] private AnimationCurve bobAnimationCurve;
		
		private IReadonlyWoodTile _data;

		public override void Bind(IReadonlyTile data)
		{
			_data = (IReadonlyWoodTile)data;
			
			_data.TraversedAway += OnTraversedOnto;
		}

		public override void Dispose()
		{
			_data.TraversedAway -= OnTraversedOnto;
		}

		private void OnTraversedOnto(GridVector direction, Xor isInverted)
		{
			if (isInverted)
			{
				return;
			}
			
			Co.Animate(bobAnimationCurve.keys.Max(k => k.time), t =>
			{
				float y = LevelDimensions.LevelBasePlaneY + bobAnimationCurve.Evaluate(t);
				transform.position = transform.position.SetY(y);
			});
		}
	}
}