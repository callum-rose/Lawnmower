using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BalsamicBits.Extensions;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Random = UnityEngine.Random;

public class BoidsManager : MonoBehaviour
{
	[SerializeField] private int count = 2;
	[SerializeField] private Transform boidPrefab;

	[SerializeField] private AnimationCurve avoidEffectByDistance;
	[SerializeField] private AnimationCurve alignEffectByDistance;
	[SerializeField] private AnimationCurve cohesionEffectByDistance;
	[SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 20);
	[SerializeField] private float boundsForceStrength = 1;
	[SerializeField] private Vector2 speedRange = new Vector2(1, 2);

	[SerializeField] private int jobCount;

	private List<Boid> _boids;
	private Dictionary<Boid, Transform> _boidTransforms;
	
	private AvoidRule _avoidRule;
	private AlignRule _alignRule;
	private CohesionRule _cohesionRule;
	private BoundsRule _boundsRule;

	private void Awake()
	{
		Init();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
	}

	private void OnValidate()
	{
		_avoidRule?.Update(avoidEffectByDistance);
		_alignRule?.Update(alignEffectByDistance);
		_cohesionRule?.Update(cohesionEffectByDistance);
		_boundsRule?.Update(bounds, boundsForceStrength);
	}

	[Button]
	private void Init()
	{
		DestroyBoids();
		
		_avoidRule = new AvoidRule(avoidEffectByDistance);
		_alignRule = new AlignRule(alignEffectByDistance);
		_cohesionRule = new CohesionRule(cohesionEffectByDistance);
		_boundsRule = new BoundsRule(bounds, boundsForceStrength);

		IBoidRule[] boidRules = { _avoidRule, _alignRule, _cohesionRule, _boundsRule };

		VelocityPostProcessor velocityProcessor = new VelocityPostProcessor(speedRange.x, speedRange.y);

		_boids = new List<Boid>(count);
		for (int i = 0; i < count; i++)
		{
			Boid boid = new Boid(
				new Vector3(
					Random.Range(bounds.min.x, bounds.max.x), 
					Random.Range(bounds.min.y, bounds.max.y),
					Random.Range(bounds.min.z, bounds.max.z)),
				Random.onUnitSphere * Random.Range(speedRange.x, speedRange.y),
				boidRules,
				velocityProcessor);

			_boids.Add(boid);
		}

		foreach (Boid boid in _boids)
		{
			IEnumerable<Boid> otherBoids = _boids.Where(b => b != boid);
			boid.SetOtherBoids(otherBoids);
		}

		_boidTransforms = new Dictionary<Boid, Transform>(_boids.Count);
		foreach (Boid boid in _boids)
		{
			Transform newTransform = Instantiate(boidPrefab);
			_boidTransforms.Add(boid, newTransform);
		}
	}

	private void DestroyBoids()
	{
		if (_boids == null)
		{
			return;
		}

		foreach (Boid boid in _boids)
		{
			Destroy(_boidTransforms[boid].gameObject);
		}

		_boids.Clear();
		_boidTransforms.Clear();
	}

	private void Update()
	{
		StartCoroutine(TickRoutine());
	}

	private IEnumerator TickRoutine()
	{
		foreach (Boid boid in _boids)
		{
			boid.Tick(Time.deltaTime);

			// Transform boidTransform = _boidTransforms[boid];
			//
			// boidTransform.position = boid.Position;
			// boidTransform.forward = boid.Velocity;
		}

		NativeArray<Boid.Data> boidData = _boids.Select(b => b.Info).ToNativeArray(Allocator.TempJob);
		SetTransformsJob transformsJob = new SetTransformsJob
		{
			BoidData = boidData
		};
		
		TransformAccessArray transformAccessArray = new TransformAccessArray(_boidTransforms.Values.ToArray(), jobCount);
		JobHandle jobHandle = transformsJob.Schedule(transformAccessArray);

		yield return new WaitForEndOfFrame();
		
		jobHandle.Complete();

		boidData.Dispose();
		transformAccessArray.Dispose();
	}
}