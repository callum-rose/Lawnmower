using Cinemachine;
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
		[SerializeField] private CinemachineTargetGroup targetGroup;
		[SerializeField] private float targetWeight;
		[SerializeField] private float targetRadius;

		private void OnEnable()
		{
			SetOceanTargets();
		}

		[Button]
		private void SetOceanTargets()
		{
			if (!targetGroup.m_Targets.IsNullOrEmpty())
			{
				foreach (CinemachineTargetGroup.Target target in targetGroup.m_Targets.ToArray())
				{
					targetGroup.RemoveMember(target.target);
				}
			}

			foreach (Transform target in oceanManager.CameraTargets)
			{
				targetGroup.AddMember(target, targetWeight, targetRadius);
			}
		}
	}
}