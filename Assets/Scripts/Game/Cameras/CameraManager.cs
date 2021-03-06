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
        [SerializeField, FormerlySerializedAs("virtualCamera")] private CinemachineVirtualCamera targetGroupVCam;
        [SerializeField] private CinemachineTargetGroup cameraTargetGroup;
        [SerializeField] private HeadlessLevelManager levelManager;
        [SerializeField] private MouseTileSelector mouseTileSelector;

        [Title("Event Channels")] 
        [SerializeField] private GameObjectEventChannel mowerCreatedEventChannel;
        [SerializeField] private TileObjectEventChannel tileCreatedEventChannel;
        [SerializeField] private GameObjectEventChannel tileWillBeDestroyedEventChannel;

        #region Unity

        private void Awake()
        {
            mouseTileSelector.Dragging += OnMouseDragging;

            mowerCreatedEventChannel.EventRaised += OnMowerCreated;
            
            tileCreatedEventChannel.EventRaised += OnTileObjectCreated;
            tileWillBeDestroyedEventChannel.EventRaised += OnTileObjectWillBeDestroyed;
        }

        private void OnDestroy()
        {
            mouseTileSelector.Dragging -= OnMouseDragging;
            
            mowerCreatedEventChannel.EventRaised -= OnMowerCreated;
            
            tileCreatedEventChannel.EventRaised -= OnTileObjectCreated;
            tileWillBeDestroyedEventChannel.EventRaised -= OnTileObjectWillBeDestroyed;
        }

        #endregion

        #region API

        public void Init(Transform mowerTransform)
        {
            //virtualCamera.Follow = virtualCamera.LookAt = mowerTransform;
        }

        public void Clear()
        {
            Init(null);
        }

        #endregion

        #region Events

        private void OnMowerCreated(GameObject gameObject)
        {
            
        }
        
        private void OnTileObjectCreated(GameObject gameObject, GridVector _)
        {
            cameraTargetGroup.AddMember(gameObject.transform, 1, LevelDimensions.TileSize * 0.5f);
        }        
        
        private void OnTileObjectWillBeDestroyed(GameObject gameObject)
        {
            if (gameObject.transform == null)
            {
                cameraTargetGroup.m_Targets = cameraTargetGroup.m_Targets
                    .Where(t => t.target != null)
                    .ToArray();
                return;
            }

            cameraTargetGroup.RemoveMember(gameObject.transform);
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
