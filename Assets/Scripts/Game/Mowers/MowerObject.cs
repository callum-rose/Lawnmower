using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Mowers.Input;
using Game.Tiles;

namespace Game.Mowers
{
	public class MowerObject : MonoBehaviour, IDataObject<MowerMover>
	{
		[SerializeField] private INeedMowerPositionContainer[] needMowerPositionContainers;
		[SerializeField] private INeedPositionerContainer[] needPositionerContainers;

		private IEnumerable<INeedMowerPosition> NeedMowerPositions => needMowerPositionContainers.Select(n => n.Result);
		private IEnumerable<INeedPositioner> NeedPositioners => needPositionerContainers.Select(n => n.Result);

		public void Init(Positioner positioner)
		{
			foreach (INeedPositioner needPositioner in NeedPositioners)
			{
				needPositioner.Set(positioner);
			}
		}

		public void Bind(MowerMover mowerMover)
		{
			foreach (INeedMowerPosition needMowerPosition in NeedMowerPositions)
			{
				needMowerPosition.Set(mowerMover);
			}
		}

		public void Dispose()
		{
			foreach (INeedMowerPosition needMowerPosition in NeedMowerPositions)
			{
				needMowerPosition.Clear();
			}
		}
	}
}