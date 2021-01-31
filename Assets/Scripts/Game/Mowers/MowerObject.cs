using Core.EventChannels;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using Sirenix.OdinInspector;

namespace Game.Mowers
{
	public class MowerObject : MonoBehaviour, IDataObject<MowerMover>
	{
		[SerializeField, InlineEditor(Expanded = true)]
		private MowerMoverData data;

		[TitleGroup("Event Channels")]
		[SerializeField] private Vector3EventChannel movedEventChannel;

		private Positioner _positioner;
		private MowerMover _mowerMover;

		private Tween _tween;

		#region API

		public void Init(Positioner positioner)
		{
			_positioner = positioner;
		}

		public void Bind(MowerMover mowerMover)
		{
			_mowerMover = mowerMover;

			_mowerMover.CurrentPosition.ValueChanged += PositionChanged;
			PositionChanged(_mowerMover.CurrentPosition.Value, true);
		}

		public void Dispose()
		{
			_mowerMover.CurrentPosition.ValueChanged -= PositionChanged;
		}

		#endregion

		#region Methods

		private void PositionChanged(GridVector position, Xor isInverted)
		{
			Vector3 worldPosition = _positioner.GetWorldPosition(position);

			_tween?.Kill();
			
			if (!isInverted)
			{
				_tween = transform
					.DOMove(worldPosition, 0.1f).OnUpdate(() => movedEventChannel.Raise(transform.position))
					.OnComplete(() => _tween = null);
			}
			else
			{
				transform.position = worldPosition;
				movedEventChannel.Raise(worldPosition);
			}
		}

		#endregion
	}
}