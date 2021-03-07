using UnityEngine;
using Cinemachine;
using UnityEngine.Serialization;
using Game.Levels;
using Game.Tiles;
using System.Linq;
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
        [SerializeField] private float mowerWeight = 10;
        
        [TitleGroup("Mower Camera")]
        [SerializeField] private CinemachineVirtualCamera mowerVCam;

        [TitleGroup("Event Channels")] 
        [SerializeField] private IGameObjectEventChannelListenerContainer mowerCreatedEventChannelContainer;
        [SerializeField] private IGameObjectEventChannelListenerContainer mowerWillBeDestroyedEventChannelContainer;
        [SerializeField] private TileObjectEventChannel tileCreatedEventChannel;
        [SerializeField] private GameObjectEventChannel tileWillBeDestroyedEventChannel;

        private IGameObjectEventChannelListener MowerCreatedEventChannel => mowerCreatedEventChannelContainer.Result;
        private IGameObjectEventChannelListener MowerWillBeDestroyedEventChannel => mowerWillBeDestroyedEventChannelContainer.Result;
        
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
            mowerVCam.Follow = mowerVCam.LookAt = gameObject.transform;
        }
        
        private void OnMowerWillBeDestroyed(GameObject gameObject)
        {
            mowerVCam.Follow = mowerVCam.LookAt = null;
        }
        
        private void OnTileObjectCreated(GameObject gameObject, GridVector _)
        {
            levelTargetGroup.AddMember(gameObject.transform, tileWeight, LevelDimensions.TileSize * 0.5f);
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

            levelTargetGroup.RemoveMember(gameObject.transform);
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
