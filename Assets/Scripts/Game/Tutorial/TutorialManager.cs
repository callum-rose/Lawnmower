using System.Collections;
using System.Collections.Generic;
using Core;
using Game.Core;
using Game.Mowers.Input;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Tutorial
{
	internal partial class TutorialManager : MonoBehaviour
	{
		[TitleGroup("Tap Arrows")]
		[SerializeField] private UnityEvent tapArrowsStartEvent;
		[SerializeField] private UnityEvent tapArrowsEndEvent;
		
		[TitleGroup("Swipe")]
		[SerializeField] private UnityEvent swipeSequenceStartEvent;
		[SerializeField] private UnityEvent swipeSequenceEndEvent;	
		
		[TitleGroup("Mow Lawn Message")]
		[SerializeField] private UnityEvent mowLawnSequenceStartEvent;
		
		[TitleGroup("Event Channels")]
		[SerializeField] private MowerInputEventChannel mowerInputEventChannel;

		private StageHandler _currentStageHandler;
		private Queue<StageHandler> _stageQueue;

		private void OnMowerInput(GridVector _)
		{
			if (_currentStageHandler.ExitTriggerType != StageHandler.EventType.MowerMove)
			{
				return;
			}

			if (_currentStageHandler.TryExit())
			{
				NextStage();
			}
		}

		[Button]
		private void Begin()
		{
			Debug.Log("Tutorial started");

			mowerInputEventChannel.EventRaised += OnMowerInput;

			StageHandler tapStageHandler = new StageHandler(
				() => tapArrowsStartEvent.Invoke(),
				1,
				StageHandler.EventType.MowerMove,
				() => tapArrowsEndEvent.Invoke());

			StageHandler swipeStageHandler = new StageHandler(
				() => swipeSequenceStartEvent.Invoke(),
				1,
				StageHandler.EventType.MowerMove,
				() => swipeSequenceEndEvent.Invoke());	
			
			StageHandler mowLawnStageHandler = new StageHandler(
				() =>
				{
					mowLawnSequenceStartEvent.Invoke();
					StartCoroutine(EndAfterDuration(5));
				},
				0,
				StageHandler.EventType.Timer,
				null);

			_stageQueue = new Queue<StageHandler>();
			_stageQueue.Enqueue(tapStageHandler);
			_stageQueue.Enqueue(swipeStageHandler);
			_stageQueue.Enqueue(mowLawnStageHandler);
			
			NextStage();
		}

		private void NextStage()
		{
			if (_stageQueue.Count == 0)
			{
				End();
				return;
			}
			
			_currentStageHandler = _stageQueue.Dequeue();
			_currentStageHandler.Enter();
		}

		private void End()
		{
			Debug.Log("Tutorial ended");

			mowerInputEventChannel.EventRaised -= OnMowerInput;
			
			PersistantData.LevelModule.TutorialCompleted.Save(true);
		}

		private IEnumerator EndAfterDuration(float duration)
		{
			float endTime = Time.time + duration;

			yield return new WaitUntil(() => Time.time >= endTime);

			_currentStageHandler.TryExit();
			
			NextStage();
		}
	}
}