using Cinemachine;
using UnityEngine;

namespace Game.Levels.Editorr
{
	internal class EditorCameraSwitcher : MonoBehaviour
	{
		[SerializeField] private new CinemachineVirtualCamera camera;

		private CinemachineTransposer _transposer;
		private Vector3 _initialFollowOffset;
		private bool _isBirdsEye;

        private void Awake()
        {
            GetGroupComposer();
        }

        public void Toggle()
        {
            if (_transposer == null)
            {
                GetGroupComposer();
            }

            _isBirdsEye = !_isBirdsEye;
            _transposer.m_FollowOffset = _isBirdsEye ? Vector3.up * 10 : _initialFollowOffset;
        }

        private void GetGroupComposer()
        {
            _transposer = camera.GetCinemachineComponent<CinemachineTransposer>();
            _initialFollowOffset = _transposer.m_FollowOffset;
        }
    }
}