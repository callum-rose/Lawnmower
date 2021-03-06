using System.Collections.Generic;
using Core;
using DG.Tweening;
using Game.Core;
using Game.Mowers.Input;
using Game.Tutorial;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityLayers = Core.UnityLayers;

namespace Game.UI
{
	internal class UiArrowsManager : MonoBehaviour
	{
		[SerializeField] private Camera camera;
		[SerializeField] private MowerInputEventChannel mowerInputEventChannel;
		[SerializeField] private SerialisedDictionary<GameObject, GridVector> arrowToDirectionDict;
		
		public void Input(Vector2 normalisedPosition)
		{
			Ray ray = camera.ViewportPointToRay(normalisedPosition);
			if (!Physics.Raycast(ray, out RaycastHit hitInfo, 1 << (int) UnityLayers.UI))
			{
				return;
			}

			hitInfo.transform.DOComplete();
			hitInfo.transform.DOPunchScale(Vector3.one * -0.2f, 0.2f, 1);

			GridVector direction = arrowToDirectionDict[hitInfo.transform.gameObject];
			mowerInputEventChannel.Raise(direction);
		}
	}
}