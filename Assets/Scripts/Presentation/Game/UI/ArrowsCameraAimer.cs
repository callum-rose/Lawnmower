using UnityEngine;

namespace Game.UI
{
    internal class ArrowsCameraAimer : MonoBehaviour
    {
        [SerializeField] private Transform mainCameraTransform;

        private void LateUpdate()
        {
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}
