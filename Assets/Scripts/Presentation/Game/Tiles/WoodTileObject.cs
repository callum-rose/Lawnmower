using System;
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
		[Serializable]
		private class Piece
		{
			[Tooltip("A child of this tile, inactive until chosen")]
			public GameObject model;

			[Tooltip("The edges the model connects across, unrotated")]
			public WoodConnections connections;
		}

		[SerializeField] private AnimationCurve bobAnimationCurve;
		[SerializeField] private Piece[] pieces;

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

		/// <summary>Shows the piece whose planks run across exactly <paramref name="connections"/>, turned to match.</summary>
		public void Connect(WoodConnections connections)
		{
			bool found = false;

			foreach (Piece piece in pieces)
			{
				bool show = false;

				for (int turns = 0; !found && turns < 4; turns++)
				{
					if (piece.connections.RotateClockwise(turns) == connections)
					{
						piece.model.transform.localRotation = Quaternion.AngleAxis(turns * 90, Vector3.up);
						show = found = true;
						break;
					}
				}

				piece.model.SetActive(show);
			}

			if (!found)
			{
				Debug.LogError($"{name}: no wood piece connects {connections}", this);
			}
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
