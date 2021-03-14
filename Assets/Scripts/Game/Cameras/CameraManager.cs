using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Game.Levels;
using Game.Tiles;
using System.Linq;
using BalsamicBits.Extensions;
using Core.EventChannels;
using Game.Core;
using Sirenix.OdinInspector;

namespace Game.Cameras
{
	internal class CameraManager : MonoBehaviour
	{
		[TitleGroup("Level Camera")]
		[SerializeField] private CinemachineTargetGroup levelTargetGroup;

		[SerializeField] private MouseTileSelector mouseTileSelector;
		[SerializeField] private float tileWeight = 1;

		[TitleGroup("Event Channels")]
		[SerializeField] private IGameObjectEventChannelListenerContainer mowerCreatedEventChannelContainer;

		[SerializeField] private IGameObjectEventChannelListenerContainer mowerWillBeDestroyedEventChannelContainer;
		[SerializeField] private TileObjectEventChannel tileCreatedEventChannel;
		[SerializeField] private GameObjectEventChannel tileWillBeDestroyedEventChannel;

		private IGameObjectEventChannelListener MowerCreatedEventChannel => mowerCreatedEventChannelContainer.Result;

		private IGameObjectEventChannelListener MowerWillBeDestroyedEventChannel =>
			mowerWillBeDestroyedEventChannelContainer.Result;

		private readonly Dictionary<GameObject, Transform> _tileToPlaceholderDict =
			new Dictionary<GameObject, Transform>();

		#region Unity

		private void Awake()
		{
			mouseTileSelector.Dragging += OnMouseDragging;

			MowerCreatedEventChannel.EventRaised += OnMowerCreated;
			MowerWillBeDestroyedEventChannel.EventRaised += OnMowerWillBeDestroyed;

			tileCreatedEventChannel.EventRaised += OnTileObjectCreated;
			tileWillBeDestroyedEventChannel.EventRaised += OnTileObjectWillBeDestroyed;
		}

		private void OnDestroy()
		{
			mouseTileSelector.Dragging -= OnMouseDragging;

			MowerCreatedEventChannel.EventRaised -= OnMowerCreated;
			MowerWillBeDestroyedEventChannel.EventRaised -= OnMowerWillBeDestroyed;

			tileCreatedEventChannel.EventRaised -= OnTileObjectCreated;
			tileWillBeDestroyedEventChannel.EventRaised -= OnTileObjectWillBeDestroyed;
		}

		#endregion

		#region Events

		private void OnMowerCreated(GameObject gameObject)
		{
		}

		private void OnMowerWillBeDestroyed(GameObject gameObject)
		{
		}

		private void OnTileObjectCreated(GameObject gameObject, GridVector _)
		{
			Transform tilePlaceholder = new GameObject(gameObject.name + " Placeholder").transform;
			tilePlaceholder.position = gameObject.transform.position;

			levelTargetGroup.AddMember(tilePlaceholder, tileWeight, LevelDimensions.TileSize * 0.5f);

			_tileToPlaceholderDict.Add(gameObject, tilePlaceholder);
		}

		private void OnTileObjectWillBeDestroyed(GameObject gameObject)
		{
			if (gameObject.transform == null)
			{
				levelTargetGroup.m_Targets = levelTargetGroup.m_Targets
					.Where(t => t.target != null)
					.ToArray();
				return;
			}

			Transform tilePlaceholder = _tileToPlaceholderDict.GetThenRemove(gameObject);
			levelTargetGroup.RemoveMember(tilePlaceholder);
			Destroy(tilePlaceholder.gameObject);
		}

		private void OnMouseDragging(bool isDragging)
		{
			//Debug.Log(isDragging);

			//    targetGroupVCam.GetCinemachineComponent<CinemachineGroupComposer>().enabled = !isDragging;

			////freezeVCam.enabled = isDragging;
		}

		#endregion
	}
}