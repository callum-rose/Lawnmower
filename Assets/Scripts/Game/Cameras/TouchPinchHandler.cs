﻿using UnityEngine;
using Cinemachine;

namespace Game.Cameras
{
    internal class TouchPinchHandler : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera gameCamera;
        [SerializeField] private float defaultCameraOrthoSize;

        #region Unity Events

        public void OnPinch(float scale)
        {
            CinemachineGroupComposer groupComposer = gameCamera.GetCinemachineComponent<CinemachineGroupComposer>();
            groupComposer.m_GroupFramingSize = defaultCameraOrthoSize * scale;
        }

        #endregion
    }
}