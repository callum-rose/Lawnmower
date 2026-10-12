
using Game.Levels;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
using BalsamicBits.Extensions;

namespace Game.Cameras
{
	internal class OceanTargetCameraManager : MonoBehaviour
	{
		[SerializeField] private OceanManager oceanManager;
		[SerializeField] private Unity.Cinemachine.CinemachineTargetGroup targetGroup;
		[SerializeField] private float targetWeight;
		[SerializeField] private float targetRadius;

		private void OnEnable()
		{
			SetOceanTargets();
		}

		[Button]
		private void SetOceanTargets()
		{
			if (!targetGroup.Targets.IsNullOrEmpty())
			{
				foreach (Unity.Cinemachine.CinemachineTargetGroup.Target target in targetGroup.Targets.ToArray())
				{
					targetGroup.RemoveMember(target.Object);
				}
			}

			foreach (Transform target in oceanManager.CameraTargets)
			{
				targetGroup.AddMember(target, targetWeight, targetRadius);
			}
		}
	}
}