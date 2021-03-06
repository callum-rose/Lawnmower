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
			
		}

		#endregion

		#region Methods
		

		#endregion
	}
}