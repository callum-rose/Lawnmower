using System;
using UnityEngine;

namespace Game.Tutorial
{
	internal partial class TutorialManager
	{
		private class StageHandler
		{
			public enum EventType
			{
				MowerMove,
				Timer
			}

			public readonly EventType ExitTriggerType;
			
			private readonly Action _enterAction;
			private readonly float _minDuration;
			private readonly Action _exitAction;

			private float _minExitTime;

			public StageHandler(Action enterAction, float minDuration, EventType exitTriggerType, Action exitAction)
			{
				_minDuration = minDuration;
				_enterAction = enterAction;
				ExitTriggerType = exitTriggerType;
				_exitAction = exitAction;
			}

			public void Enter()
			{
				_enterAction?.Invoke();
				_minExitTime = Time.time + _minDuration;
			}

			public bool TryExit()
			{
				if (Time.time < _minExitTime)
				{
					return false;
				}

				_exitAction?.Invoke();
				return true;
			}
		}
	}
}