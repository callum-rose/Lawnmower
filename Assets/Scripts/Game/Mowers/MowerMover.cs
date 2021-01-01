using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Sirenix.OdinInspector;
using Vector3 = UnityEngine.Vector3;

namespace Game.Mowers
{
	internal class MowerMover : MonoBehaviour
	{
		[SerializeField, InlineEditor(Expanded = true)]
		private MowerMoverData data;

		public GridVector CurrentPosition { get; private set; }

		private Positioner _positioner;

		private readonly Queue<Vector3> _positionQueue = new Queue<Vector3>();
		private float _pathFractionComplete;

		#region Unity

		private void Update()
		{
			if (_positionQueue.Count == 0)
			{
				return;
			}
			
			float speed = _positionQueue.Count * Time.deltaTime;
			if (speed > 0)
			{
				float floatIndex = _pathFractionComplete * _positionQueue.Count;
				floatIndex += speed;
				_pathFractionComplete = floatIndex / _positionQueue.Count;
			}

			transform.position = GetTargetPositionOnPath(_pathFractionComplete, _positionQueue);
		}

		#endregion

		#region API

		public void Init(Positioner positioner)
		{
			_positioner = positioner;
		}

		public void Move(GridVector toPosition, bool animated = true)
		{
			CurrentPosition = toPosition;

			Vector3 worldPosition = _positioner.GetWorldPosition(toPosition);
			EnqueueToTargetPath(worldPosition);
		}

		public void Bump(GridVector toPosition)
		{
		}

		#endregion

		#region Routines

		#endregion

		#region Methods

		private static Vector3 GetTargetPositionOnPath(float pathFractionComplete, IEnumerable<Vector3> positionQueue)
		{
			if (positionQueue.Count() == 0)
			{
				return Vector3.zero;
			}
			
			if (pathFractionComplete >= 1f)
			{
				return positionQueue.Last();
			}

			float floatIndex = pathFractionComplete * positionQueue.Count();
			(int prevIndex, int postIndex) = (Mathf.FloorToInt(floatIndex), Mathf.CeilToInt(floatIndex));

			if (prevIndex == postIndex)
			{
				return positionQueue.ElementAt(prevIndex);
			}

			Vector3 prevPosition, postPosition;
			using (IEnumerator<Vector3> enumerator = positionQueue.GetEnumerator())
			{
				for (int i = 0; i <= prevIndex; i++)
				{
					enumerator.MoveNext();
				}

				prevPosition = enumerator.Current;
				enumerator.MoveNext();
				postPosition = enumerator.Current;
			}

			float t = floatIndex - prevIndex;
			return Vector3.Lerp(prevPosition, postPosition, t);
		}

		private void EnqueueToTargetPath(Vector3 position)
		{
			_positionQueue.Enqueue(position);
			_pathFractionComplete *= (_positionQueue.Count - 1f) / _positionQueue.Count;
		}

		private void DequeueFromTargetPath()
		{
			_positionQueue.Dequeue();
			_pathFractionComplete *= _positionQueue.Count / (_positionQueue.Count + 1f);
		}

		#endregion
	}
}