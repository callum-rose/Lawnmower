using System;
using System.Linq;
using BalsamicBits.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Tiles
{
	public class PrefabArranger : MonoBehaviour
	{
		[TitleGroup("Inputs")] [SerializeField]
		private WeightedPrefab[] prefabs;

		[SerializeField] private Vector2Int resolution = new Vector2Int(5, 5);
		[SerializeField] private Vector3 size = new Vector3(1, 0, 1);
		[SerializeField] private Vector3 positionOffset;
		[SerializeField] private Vector3 randomPositionOffset;
		[SerializeField] private bool doRandomRotation;

		[SerializeField, Indent, ShowIf(nameof(doRandomRotation))]
		private Vector3 randomRotation;

		[SerializeField] private bool doRandomScale;

		[SerializeField, Indent, ShowIf(nameof(doRandomScale))]
		private bool scaleUniform;

		[SerializeField, LabelText("Scale Factor"), Indent, ShowIf(nameof(doRandomScale)), ShowIf(nameof(scaleUniform))]
		private float scaleFactorSingle = 1;

		[SerializeField, LabelText("Scale Factor"), Indent, ShowIf(nameof(doRandomScale)), HideIf(nameof(scaleUniform))]
		private Vector3 randomScaleFactorVec = Vector3.one;

		[TitleGroup("Instantiated")] [SerializeField]
		private Transform[] transforms;

#if UNITY_EDITOR

		private void OnValidate()
		{
			if (prefabs.IsNullOrEmpty())
			{
				return;
			}

			if (prefabs.Length == 1)
			{
				prefabs[0].weight = 1;
			}
			
			float totalWeight = prefabs.Sum(p => p.weight);
			for (int index = 0; index < prefabs.Length; index++)
			{
				WeightedPrefab p = prefabs[index];
				p.totalWeight = totalWeight;
				prefabs[index] = p;
			}
		}

		[TitleGroup("Actions")]
		[Button]
		private void Create()
		{
			int GetWeightedRandomPrefabIndex()
			{
				float randomWeight = Random.value;
				float sumWeight = 0;
				int index = 0;
				for (int i = 0; i < prefabs.Length; i++)
				{
					WeightedPrefab wp = prefabs[i];
					float weight = wp.weight;
					sumWeight += weight;
					if (randomWeight < sumWeight)
					{
						index = i;
						break;
					}
				}

				return index;
			}

			transforms = new Transform[resolution.x * resolution.y];
			Utils.Loops.TwoD(resolution.x, resolution.y, (x, z) =>
			{
				int index = GetWeightedRandomPrefabIndex();

				Transform newTransform =
					(UnityEditor.PrefabUtility.InstantiatePrefab(prefabs[index].prefab, transform) as GameObject)
					.transform;

				Arrange(newTransform, x, z);

				transforms[x + resolution.x * z] = newTransform;
			});
		}

		[Button]
		private void ReArrange()
		{
			Utils.Loops.TwoD(resolution.x, resolution.y, (x, z) =>
			{
				Transform transform = transforms[x + resolution.x * z];
				Arrange(transform, x, z);
			});
		}

		[Button]
		private void DestroyAllChildren()
		{
			while (transform.childCount > 0)
			{
				Transform child = transform.GetChild(0);
				if (Application.isPlaying)
				{
					Destroy(child.gameObject);
				}
				else
				{
					DestroyImmediate(child.gameObject);
				}
			}
		}

		private void Arrange(Transform transform, int x, int z)
		{
			float newX = ((x / (resolution.x - 1f)) - 0.5f) * size.x;
			float newZ = ((z / (resolution.y - 1f)) - 0.5f) * size.z;

			transform.localPosition =
				new Vector3(newX, 0, newZ) + positionOffset + randomPositionOffset.RandomiseInRange();
			if (doRandomRotation)
			{
				float GetAngle(float range) => Random.Range(-range, range);
				transform.localRotation =
					Quaternion.Euler(GetAngle(randomRotation.x), GetAngle(randomRotation.y),
						GetAngle(randomRotation.z)) * transform.localRotation;
			}

			if (doRandomScale)
			{
				float GetScaleRandom(float factor) => Random.Range(1f / factor, factor);
				Vector3 scaleFactorVec;
				if (scaleUniform)
				{
					scaleFactorVec = Vector3.one * GetScaleRandom(scaleFactorSingle);
				}
				else
				{
					scaleFactorVec = new Vector3(GetScaleRandom(randomScaleFactorVec.x),
						GetScaleRandom(randomScaleFactorVec.y), GetScaleRandom(randomScaleFactorVec.z));
				}

				transform.localScale = transform.localScale.PerElementMultiply(scaleFactorVec);
			}
		}


		
#endif

		[Serializable]
		private struct WeightedPrefab
		{
			[HorizontalGroup(0.6f), LabelWidth(50)]
			public GameObject prefab;

			[HorizontalGroup(0.25f), LabelWidth(50)]
			public float weight;

			[HideInInspector] public float totalWeight;

			[HorizontalGroup(0.15f)]
			[ShowInInspector, HideLabel]
			public float NormalisedWeight => weight / totalWeight;
		}
	}
}