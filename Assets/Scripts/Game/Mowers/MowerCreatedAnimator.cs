using System.Collections;
using Core;
using Core.EventChannels;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(MowerCreatedAnimator),
		menuName = SoNames.GameDir + nameof(MowerCreatedAnimator))]
	[UnreferencedScriptableObject]
	internal class MowerCreatedAnimator : ScriptableObjectWithCoroutines
	{
		[SerializeField] private float dropFromHeight = 10;
		[SerializeField] private float dropDuration = 0.5f;
		[SerializeField] private Ease animEase = Ease.OutBounce;

		[TitleGroup("Event Channels")]
		[SerializeField] private IVector3EventChannelListenerContainer mowerObjectMovedEventChannelContainer;

		[SerializeField] private IGameObjectEventChannelListenerContainer mowerCreatedEventChannelContainer;

		[SerializeField]
		private IBoolEventChannelTransmitterContainer isMowerTransformControlledExternallyEventChannelContainer;

		[SerializeField] private IVoidEventChannelListenerContainer tileAnimationsFinishedEventChannel;
		
		private IVector3EventChannelListener MowerObjectMovedEventChannel =>
			mowerObjectMovedEventChannelContainer.Result;

		private IGameObjectEventChannelListener MowerCreatedEventChannel => mowerCreatedEventChannelContainer.Result;

		private IVoidEventChannelListener TileAnimationsFinishedEventChannel =>
			tileAnimationsFinishedEventChannel.Result;

		private IBoolEventChannelTransmitter IsMowerTransformControlledExternallyEventChannel =>
			isMowerTransformControlledExternallyEventChannelContainer.Result;

		private void OnEnable()
		{
			MowerCreatedEventChannel.EventRaised += OnMowerCreated;
		}

		private void OnDisable()
		{
			MowerCreatedEventChannel.EventRaised -= OnMowerCreated;
		}

		private void OnMowerCreated(GameObject mower)
		{
			mower.SetActive(false);			
			StartCoroutine(WaitForAnimationRoutine(mower));
		}

		private IEnumerator WaitForAnimationRoutine(GameObject mower)
		{
			Vector3? mowerPosition = null;
			bool tileAnimationsFinished = false;

			void OnMowerMoved(Vector3 position) => mowerPosition = position;
			void OnTileAnimationsFinished() => tileAnimationsFinished = true;

			MowerObjectMovedEventChannel.EventRaised += OnMowerMoved;
			TileAnimationsFinishedEventChannel.EventRaised += OnTileAnimationsFinished;

			bool CanAnimate() => mowerPosition.HasValue && tileAnimationsFinished;

			yield return new WaitUntil(CanAnimate);

			MowerObjectMovedEventChannel.EventRaised -= OnMowerMoved;
			TileAnimationsFinishedEventChannel.EventRaised -= OnTileAnimationsFinished;

			mower.SetActive(true);			
			AnimateMower(mower, mowerPosition.Value);
		}
		private void AnimateMower(GameObject mower, Vector3 toPosition)
		{		
			mower.transform.position += Vector3.up * dropFromHeight;

			IsMowerTransformControlledExternallyEventChannel.Raise(true);

			mower.transform
				.DOMove(toPosition, dropDuration)
				.SetEase(animEase)
				.OnComplete(() => IsMowerTransformControlledExternallyEventChannel.Raise(false));
		}
	}
}