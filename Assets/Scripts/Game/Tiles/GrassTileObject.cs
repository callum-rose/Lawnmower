using Core.EventChannels;
using Game.Core;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[RequireComponent(typeof(IAppearanceSetter))]
	internal class GrassTileObject : BaseTileObject
	{
		[SerializeField] private DirtAppearanceSetter dirtAppearanceSetter;
		[SerializeField, AssetsOnly] private Vector3AndIntEventChannel grassParticlesEventChannel;
		[SerializeField, AssetsOnly] private Vector3EventChannel dirtParticlesEventChannel;

		[SerializeField, Range(0, GrassTile.MaxGrassHeight), OnValueChanged(nameof(OnDisplayGrassHeightChanged))]
		private int displayGrassHeight;

		private GrassTile _tileData;
		
		private IAppearanceSetter _appearanceSetter;

		private GridVector _lastTraverseOntoDirection;

		#region Unity

		private void Awake()
		{
			_appearanceSetter = GetComponent<IAppearanceSetter>();
			SetAppearance(displayGrassHeight);
		}

		#endregion
		
		#region API

		public override void Bind(Tile tileData)
		{
			_tileData = (GrassTile)tileData;

			_tileData.GrassHeight.ValueChangedFromTo += OnGrassHeightValueChanged;
			SetAppearance(_tileData.GrassHeight.Value);

			_tileData.TraversedOnto += OnTraversedOnto;
			_tileData.TraversedAway += OnTraversedAway;
			_tileData.BumpedInto += OnBumpedInto;
		}

		public override void Dispose()
		{
			if (_tileData == null)
			{
				return;
			}
			
			_tileData.GrassHeight.ValueChangedFromTo -= OnGrassHeightValueChanged;
			
			_tileData.TraversedOnto -= OnTraversedOnto;
			_tileData.TraversedAway -= OnTraversedAway;
			_tileData.BumpedInto -= OnBumpedInto;
		}

		#endregion

		#region Events

		private void OnGrassHeightValueChanged(int prevGrassHeight, int grassHeight, Xor isInverted)
		{			
			if (!isInverted)
			{
				if (prevGrassHeight < GrassTile.PerfectGrassHeight)
				{
					dirtParticlesEventChannel.Raise(transform.position);
				}
				else
				{
					grassParticlesEventChannel.Raise(transform.position, _tileData.GrassHeight.Value + 1);
				}
			}
			
			SetAppearance(grassHeight);
		}

		private void OnTraversedOnto(GridVector fromDirection, Xor isInverted)
		{
			dirtAppearanceSetter.Set(fromDirection);
			
			_lastTraverseOntoDirection = fromDirection;
		}

		private void OnTraversedAway(GridVector toDirection, Xor isInverted)
		{
			if (isInverted)
			{
				return;
			}

			if (_tileData.GrassHeight.Value > 0)
			{
				return;
			}

			dirtAppearanceSetter.Set(_lastTraverseOntoDirection, toDirection);
		}

		private void OnBumpedInto(GridVector direction, Xor isInverted)
		{
		}

		#endregion
		
		#region Methods

		private void SetAppearance(int grassHeight)
		{
			if (_appearanceSetter == null)
			{
				_appearanceSetter = GetComponent<IAppearanceSetter>();
			}

			_appearanceSetter.SetAppearance(grassHeight);
		}
		
		private void OnDisplayGrassHeightChanged(int _)
		{
			SetAppearance(displayGrassHeight);
		}

		#endregion
	}
}