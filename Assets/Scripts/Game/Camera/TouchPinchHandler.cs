using UnityEngine;
using Cinemachine;

namespace Game.Camera
{
    internal class TouchPinchHandler : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera gameCamera;
        [SerializeField] private float defaultCameraOrthoSize;

        #region Unity Events

        public void OnPinch(float scale)
        {
            var groupComposer = gameCamera.GetCinemachineComponent<CinemachineGroupComposer>();
            groupComposer.m_GroupFramingSize = defaultCameraOrthoSize * scale;
        }

        #endregion
    }
}