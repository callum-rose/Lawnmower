using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using Vector3 = UnityEngine.Vector3;

namespace Game.Mowers
{
	public class MowerObject : MonoBehaviour, IDataObject<MowerMoverr>
	{
		[SerializeField, InlineEditor(Expanded = true)]
		private MowerMoverData data;

		private Positioner _positioner;
		private MowerMoverr _mowerMover;

		#region API

		public void Init(Positioner positioner)
		{
			_positioner = positioner;
		}

		public void Bind(MowerMoverr mowerMover)
		{
			_mowerMover = mowerMover;
			
			_mowerMover.CurrentPosition.ValueChanged += PositionChanged;
			PositionChanged(_mowerMover.CurrentPosition.Value);
		}

		public void Dispose()
		{
			_mowerMover.CurrentPosition.ValueChanged -= PositionChanged;
		}

		#endregion

		#region Methods

		private void PositionChanged(GridVector position)
		{
			transform.position = _positioner.GetWorldPosition(position);
		}

		#endregion
	}
}