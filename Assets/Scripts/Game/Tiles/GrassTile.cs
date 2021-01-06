using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Game.Core;
using Game.UndoSystem;
using Core;

namespace Game.Tiles
{
	[RequireComponent(typeof(IAppearanceSetter))]
	internal partial class GrassTile : Tile
	{
		[SerializeField] private DirtAppearanceSetter dirtAppearanceSetter;
		[SerializeField, AssetsOnly] private Vector3AndIntEventChannel grassParticlesEventChannel;
		[SerializeField, AssetsOnly] private Vector3EventChannel dirtParticlesEventChannel;
		[SerializeField] private int internalGrassHeight;

		public const int MaxGrassHeight = 3;
		public const int PerfectGrassHeight = 1;

		public event UndoableAction Ruined;
		public override bool IsComplete => GrassHeight == PerfectGrassHeight;

		public int GrassHeight => Mathf.Clamp(internalGrassHeight, 0, MaxGrassHeight);

		private static MaterialPropertyBlock _propertyBlock;

		private IAppearanceSetter _appearanceSetter;

		private GridVector _lastTraverseOntoDirection;

		#region Unity

		private void Awake()
		{
			_appearanceSetter = GetComponent<IAppearanceSetter>();
		}

		private void OnValidate()
		{
			SetAppearance(internalGrassHeight);
		}

		#endregion

		#region API

		public override void Setup(BaseTileSetupData data)
		{
			if (!(data is GrassTileSetupData grassData))
			{
				throw new ArgumentException($"Argument must be of type {nameof(GrassTileSetupData)}");
			}

			internalGrassHeight = grassData.grassHeight;
			SetAppearance(internalGrassHeight);
		}

		public override bool IsTraversable(bool editMode)
		{
			if (!editMode)
			{
				return true;
			}
			else
			{
				return GrassHeight < MaxGrassHeight;
			}
		}

		public override void TraverseOnto(GridVector fromDirection, Xor inverted)
		{
			void CheckInvokeRuinedEvent()
			{
				if (GrassHeight == PerfectGrassHeight)
				{
					// ReSharper disable once PossibleNullReferenceException
					Ruined.Invoke(inverted);
				}
			}

			void TriggerParticleEvent()
			{
				if (GrassHeight == 0)
				{
					dirtParticlesEventChannel.Raise(transform.position);
				}
				else
				{
					grassParticlesEventChannel.Raise(transform.position, internalGrassHeight);
				}
			}

			void IncrementGrassHeight()
			{
				internalGrassHeight += inverted ? 1 : -1;
			}

			if (!inverted)
			{
				CheckInvokeRuinedEvent();
				TriggerParticleEvent();
				IncrementGrassHeight();
			}
			else
			{
				if (internalGrassHeight + 1 > MaxGrassHeight)
				{
					throw new InvalidOperationException("Grass height too high");
				}

				IncrementGrassHeight();
				CheckInvokeRuinedEvent();
			}

			dirtAppearanceSetter.Set(fromDirection);
			SetAppearance(internalGrassHeight);

			_lastTraverseOntoDirection = fromDirection;
		}

		public override void TraverseAway(GridVector toDirection, Xor inverted)
		{
			if (!inverted)
			{
				if (GrassHeight > 0)
				{
					return;
				}

				dirtAppearanceSetter.Set(_lastTraverseOntoDirection, toDirection);
			}
			else
			{
			}
		}

		#endregion

		#region Methods

		private void SetAppearance(int grassHeight)
		{
			if (_appearanceSetter == null)
			{
				_appearanceSetter = GetComponent<IAppearanceSetter>();
			}

			internalGrassHeight = grassHeight;
			_appearanceSetter.SetAppearance(this);
		}

		#endregion
	}
}