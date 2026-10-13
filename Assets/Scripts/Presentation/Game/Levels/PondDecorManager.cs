using System;
using System.Collections.Generic;
using BalsamicBits.Extensions;
using Game.Core;
using Lawnmower.Model;
using UnityEngine;
using Random = System.Random;

namespace Game.Levels
{
	/// <summary>
	/// Scatters pond props and creatures on the ocean around each level, clear of the board. How many there are scales
	/// with the level's tile count. The layout is seeded by the level's id, so a level always looks the same.
	/// </summary>
	internal class PondDecorManager : MonoBehaviour
	{
		[Serializable]
		private class Decor
		{
			public GameObject prefab;
			public int maxCount = 1;
			[Tooltip("How likely each pick is to be this decor, relative to the others.")]
			public float weight = 1;
			public Vector2 scale = new Vector2(0.5f, 0.5f);
			[Tooltip("Unscaled radius the model needs around its origin.")]
			public float footprint = 0.6f;
			[Tooltip("Height of the origin above the water surface, unscaled.")]
			public float heightAboveWater;
			[Tooltip("Unscaled height of the top surface a percher sits on; 0 if nothing can perch on it.")]
			public float perchHeight;
			[Tooltip("Sits on top of an earlier decor with a perch height, instead of on the water.")]
			public bool perches;
			[Tooltip("Swims in a slow circle of this radius (world units) around where it's placed; 0 to stay put.")]
			public float swimRadius;
		}

		private class Spawned
		{
			public GameObject gameObject;
			public float scale;
			public float distance;
			public float shown;
		}

		private struct Placed
		{
			public Vector3 position;
			public float radius;
			public float perchTop;
		}

		[SerializeField] private Positioner positioner;
		[SerializeField] private OceanManager oceanManager;
		[Tooltip("Decor per level tile, rounded: 3/16 gives a 4 x 4 level 3 decor.")]
		[SerializeField] private float decorPerTile = 3f / 16;
		[SerializeField] private float boardMargin = 0.4f;
		[SerializeField] private float oceanEdgeMargin = 0.3f;
		[SerializeField] private float spacing = 0.1f;
		[SerializeField] private int attemptsPerDecor = 30;
		[SerializeField] private float swimSpeed = 0.3f;
		[Tooltip("Time to grow in once the ocean has spread out to it.")]
		[SerializeField] private float appearDuration = 0.25f;
		[SerializeField] private List<Decor> decors = new List<Decor>();

		[Header("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelDataEventChannel;

		private readonly List<Spawned> _spawned = new List<Spawned>();
		private Vector3 _center;

		#region Unity

		private void Awake()
		{
			levelDataEventChannel.EventRaised += LevelDataEventChannelOnEventRaised;
		}

		private void Update()
		{
			float showingRadius = oceanManager.ShowingRadius;
			float step = Time.deltaTime / appearDuration;

			foreach (Spawned spawned in _spawned)
			{
				spawned.shown = Mathf.MoveTowards(spawned.shown, spawned.distance < showingRadius ? 1 : 0, step);
				spawned.gameObject.SetActive(spawned.shown > 0);
				spawned.gameObject.transform.localScale = Vector3.one * (spawned.scale * EaseOutBack(spawned.shown));
			}
		}

		private void OnDestroy()
		{
			levelDataEventChannel.EventRaised -= LevelDataEventChannelOnEventRaised;
		}

		#endregion

		#region Events

		private void LevelDataEventChannelOnEventRaised(Level level)
		{
			Clear();
			Build(level);
		}

		#endregion

		#region Methods

		private void Clear()
		{
			foreach (Spawned spawned in _spawned)
			{
				Destroy(spawned.gameObject);
			}

			_spawned.Clear();
		}

		private void Build(Level level)
		{
			Random random = new Random(level.Id.GetHashCode());
			Vector3 center = _center = positioner.GetLevelWorldCenter(level);
			float oceanRadius = oceanManager.GetRadius(level) - oceanEdgeMargin;
			float waterY = oceanManager.transform.position.y;
			Rect board = GetBoardRect(level);
			List<Placed> placed = new List<Placed>();
			int[] counts = PickCounts(random, Mathf.RoundToInt(level.Width * level.Depth * decorPerTile));

			for (int d = 0; d < decors.Count; d++)
			{
				Decor decor = decors[d];

				for (int i = 0; i < counts[d]; i++)
				{
					float scale = Mathf.Lerp(decor.scale.x, decor.scale.y, (float)random.NextDouble());
					Quaternion rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);

					if (decor.perches)
					{
						if (TryTakePerch(placed, random, out Vector3 perch))
						{
							Spawn(decor, perch + Vector3.up * decor.heightAboveWater * scale, rotation, scale);
						}

						continue;
					}

					float radius = decor.footprint * scale + decor.swimRadius;

					if (TryFindSpot(random, center, oceanRadius - radius, board, radius, placed, out Vector3 position))
					{
						position.y = waterY + decor.heightAboveWater * scale;
						GameObject spawned = Spawn(decor, position, rotation, scale);

						if (decor.swimRadius > 0)
						{
							spawned.AddComponent<PondSwimmer>().Init(decor.swimRadius, swimSpeed, (float)random.NextDouble());
						}

						placed.Add(new Placed
						{
							position = position.SetY(waterY),
							radius = radius,
							perchTop = decor.perchHeight > 0 ? position.y + decor.perchHeight * scale : float.NaN,
						});
					}
				}
			}
		}

		/// <summary>
		/// Picks <paramref name="total"/> decor by weight, each up to its max count. A percher is only picked once there's
		/// a free perch picked for it.
		/// </summary>
		private int[] PickCounts(Random random, int total)
		{
			int[] counts = new int[decors.Count];
			int perches = 0;
			int perchers = 0;

			bool CanPick(int index)
			{
				Decor decor = decors[index];
				return decor.prefab != null && decor.weight > 0 && counts[index] < decor.maxCount &&
					(!decor.perches || perchers < perches);
			}

			for (int picked = 0; picked < total; picked++)
			{
				float totalWeight = 0;

				for (int i = 0; i < decors.Count; i++)
				{
					totalWeight += CanPick(i) ? decors[i].weight : 0;
				}

				if (totalWeight <= 0)
				{
					break;
				}

				float pick = (float)random.NextDouble() * totalWeight;
				int index = 0;

				for (; index < decors.Count - 1; index++)
				{
					if (!CanPick(index))
					{
						continue;
					}

					pick -= decors[index].weight;

					if (pick < 0)
					{
						break;
					}
				}

				while (!CanPick(index))
				{
					index--;
				}

				counts[index]++;
				perches += decors[index].perchHeight > 0 ? 1 : 0;
				perchers += decors[index].perches ? 1 : 0;
			}

			return counts;
		}

		private Rect GetBoardRect(Level level)
		{
			Vector3 min = positioner.GetWorldPosition(new GridVector(0, 0));
			Vector3 max = positioner.GetWorldPosition(new GridVector(level.Width - 1, level.Depth - 1));
			float inset = LevelDimensions.TileSize / 2 + boardMargin;

			return Rect.MinMaxRect(min.x - inset, min.z - inset, max.x + inset, max.z + inset);
		}

		private bool TryFindSpot(Random random, Vector3 center, float maxDistance, Rect board, float radius,
			List<Placed> placed, out Vector3 position)
		{
			for (int attempt = 0; attempt < attemptsPerDecor && maxDistance > 0; attempt++)
			{
				float angle = (float)random.NextDouble() * Mathf.PI * 2;
				float distance = Mathf.Sqrt((float)random.NextDouble()) * maxDistance;
				position = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;

				if (DistanceToRect(board, position) >= radius && !Overlaps(position, radius, placed))
				{
					return true;
				}
			}

			position = default;
			return false;
		}

		private bool Overlaps(Vector3 position, float radius, List<Placed> placed)
		{
			foreach (Placed other in placed)
			{
				float minDistance = radius + other.radius + spacing;

				if ((other.position - position).SetY(0).sqrMagnitude < minDistance * minDistance)
				{
					return true;
				}
			}

			return false;
		}

		private static float DistanceToRect(Rect rect, Vector3 position)
		{
			float dx = Mathf.Max(rect.xMin - position.x, 0, position.x - rect.xMax);
			float dz = Mathf.Max(rect.yMin - position.z, 0, position.z - rect.yMax);
			return Mathf.Sqrt(dx * dx + dz * dz);
		}

		private static bool TryTakePerch(List<Placed> placed, Random random, out Vector3 perch)
		{
			int start = random.Next(placed.Count);

			for (int i = 0; i < placed.Count; i++)
			{
				int index = (start + i) % placed.Count;
				Placed candidate = placed[index];

				if (!float.IsNaN(candidate.perchTop))
				{
					perch = candidate.position.SetY(candidate.perchTop);
					candidate.perchTop = float.NaN;
					placed[index] = candidate;
					return true;
				}
			}

			perch = default;
			return false;
		}

		private GameObject Spawn(Decor decor, Vector3 position, Quaternion rotation, float scale)
		{
			GameObject spawned = Instantiate(decor.prefab, position, rotation, transform);
			spawned.SetActive(false);
			_spawned.Add(new Spawned
			{
				gameObject = spawned,
				scale = scale,
				distance = (position - _center).SetY(0).magnitude,
			});
			return spawned;
		}

		private static float EaseOutBack(float t)
		{
			const float overshoot = 1.7f;
			t -= 1;
			return 1 + t * t * ((overshoot + 1) * t + overshoot);
		}

		#endregion
	}
}
