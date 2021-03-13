using Core;
using Core.EventChannels;
using DG.Tweening;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(MowerCreatedAnimator),
		menuName = SoNames.GameDir + nameof(MowerCreatedAnimator))]
	[UnreferencedScriptableObject]
	internal class MowerCreatedAnimator : ScriptableObject
	{
		[SerializeField] private GamePlayData gamePlayData;
		[SerializeField] private float dropFromHeight = 10;
		[SerializeField] private float dropDuration = 0.5f;
		[SerializeField] private Ease animEase = Ease.OutBounce;

		[TitleGroup("Event Channels")]
		[SerializeField] private IVector3EventChannelListenerContainer mowerObjectMovedEventChannelContainer;
		[SerializeField] private IGameObjectEventChannelListenerContainer mowerCreatedEventChannelContainer;
		[SerializeField] private IBoolEventChannelTransmitterContainer isMowerTransformControlledExternallyEventChannelContainer;

		[ShowInInspector] private float DropDelay => gamePlayData.LevelIntroDuration - dropDuration;
		
		private IVector3EventChannelListener MowerObjectMovedEventChannel => mowerObjectMovedEventChannelContainer.Result;
		private IGameObjectEventChannelListener MowerCreatedEventChannel => mowerCreatedEventChannelContainer.Result;
		
		private IBoolEventChannelTransmitter IsMowerTransformControlledExternallyEventChannel =>
			isMowerTransformControlledExternallyEventChannelContainer.Result;

		private bool NeedsToAnimate => _mowerGameObject != null;
		
		private GameObject _mowerGameObject;

		private void OnEnable()
		{
			MowerObjectMovedEventChannel.EventRaised += MowerObjectMovedEventChannelOnEventRaised;
			MowerCreatedEventChannel.EventRaised += OnMowerCreated;
		}

		private void OnDisable()
		{
			MowerObjectMovedEventChannel.EventRaised -= MowerObjectMovedEventChannelOnEventRaised;
			MowerCreatedEventChannel.EventRaised -= OnMowerCreated;
		}
		
		private void MowerObjectMovedEventChannelOnEventRaised(Vector3 position)
		{
			if (!NeedsToAnimate)
			{
				return;
			}
			
			IsMowerTransformControlledExternallyEventChannel.Raise(true);

			_mowerGameObject.transform.position += Vector3.up * dropFromHeight;
			_mowerGameObject.transform
				.DOMove(position, dropDuration)
				.SetDelay(DropDelay)
				.SetEase(animEase)
				.OnComplete(() => IsMowerTransformControlledExternallyEventChannel.Raise(false));

			_mowerGameObject = null;
		}

		private void OnMowerCreated(GameObject gameObject)
		{
			_mowerGameObject = gameObject;
		}
	}
}