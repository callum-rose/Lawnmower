using Core.EventChannels;
using Game.Core;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Tiles
{
	[RequireComponent(typeof(IAppearanceSetter))]
	internal class GrassTileObject : BaseTileObject
	{
		[SerializeField] private Vector3AndIntEventChannel grassParticlesEventChannel;
		[SerializeField] private Vector3EventChannel dirtParticlesEventChannel;

		[SerializeField, Range(0, GrassTile.MaxGrassHeight)]
		private int displayGrassHeight;

		private IReadonlyGrassTile _tileData;
		
		private IAppearanceSetter _appearanceSetter;

		#region Unity

		private void Awake()
		{
			_appearanceSetter = GetComponent<IAppearanceSetter>();
			SetAppearance(displayGrassHeight);
		}

		#endregion
		
		#region API

		public override void Bind(IReadonlyTile tileData)
		{
			_tileData = (IReadonlyGrassTile)tileData;

			_tileData.GrassHeight.ValueChangedFromTo += OnGrassHeightValueChanged;
			SetAppearance(_tileData.GrassHeight.Value);
		}

		public override void Dispose()
		{
			if (_tileData == null)
			{
				return;
			}
			
			_tileData.GrassHeight.ValueChangedFromTo -= OnGrassHeightValueChanged;
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
			
			GetAppearanceSetter().TransitionAppearance(prevGrassHeight, grassHeight);
		}

		#endregion
		
		#region Methods

		private void SetAppearance(int grassHeight)
		{
			GetAppearanceSetter().SetAppearance(grassHeight);
		}

		private IAppearanceSetter GetAppearanceSetter()
		{
			if (_appearanceSetter == null)
			{
				_appearanceSetter = GetComponent<IAppearanceSetter>();
			}

			return _appearanceSetter;
		}
		

		#endregion
	}
}