using UnityEngine;
using Cinemachine;
using UnityEngine.Serialization;
using Game.Levels;
using Game.Tiles;
using System.Linq;

namespace Game.Cameras
{
    internal class CameraManager : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("virtualCamera")] private CinemachineVirtualCamera targetGroupVCam;
        [SerializeField] private CinemachineTargetGroup cameraTargetGroup;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private MouseTileSelector mouseTileSelector;

        #region Unity

        private void Awake()
        {
            // levelManager.TileAdded += LevelManager_TileAdded;
            // levelManager.TileDestroyed += LevelManager_TileDestroyed;

            mouseTileSelector.Dragging += OnMouseDragging;
        }

        private void OnDestroy()
        {
            // levelManager.TileAdded -= LevelManager_TileAdded;
            // levelManager.TileDestroyed -= LevelManager_TileDestroyed;

            mouseTileSelector.Dragging -= OnMouseDragging;
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

        private void LevelManager_TileAdded(Transform tileTransform)
        {
            cameraTargetGroup.AddMember(tileTransform, 1, LevelDimensions.TileSize * 0.5f);
        }

        private void LevelManager_TileDestroyed(Transform tileTransform)
        {
            if (tileTransform == null)
            {
                cameraTargetGroup.m_Targets = cameraTargetGroup.m_Targets
                    .Where(t => t.target != null)
                    .ToArray();
                return;
            }

            cameraTargetGroup.RemoveMember(tileTransform);
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
