using Core;
using DG.Tweening;
using UnityEngine;

namespace Game.UI
{
	internal class UiArrowsCameraRaycaster : MonoBehaviour
	{
		[SerializeField] private Camera camera;
		
		[SerializeField] private float test0;
		[SerializeField] private float test1;
		[SerializeField] private int test2;
		[SerializeField] private float test3;

		public void Raycast(Vector2 position)
		{
			Ray ray = camera.ViewportPointToRay(position);
			if (Physics.Raycast(ray, out RaycastHit hitInfo, 1 << (int) UnityLayers.UI))
			{
				hitInfo.transform.DOPunchScale(Vector3.one * test0, test1, test2, test3);
			}
		}
	}
}