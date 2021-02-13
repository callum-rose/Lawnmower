using System;
using System.Collections;
using System.Collections.Generic;
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
	[SerializeField] private AnimationCurve boundsEffectByDistance;
	[SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 20);
	[SerializeField] private Vector2 speedRange = new Vector2(1, 2);

	[SerializeField] private int jobCount;

	private List<Boid> _boids;
	private Dictionary<Boid, Transform> _boidTransforms;

	private NativeArray<Boid.Data> _boidsData;
	private NativeArray<Vector3> _avoidResultAccelerations;
	private NativeArray<Vector3> _boundsResultAccelerations;
	private NativeArray<Vector3> _alignResultAccelerations;
	private NativeArray<Vector3> _cohesionResultAccelerations;
	private TransformAccessArray _transformAccessArray;

	private JobHandle _jobHandle;

	private void Awake()
	{
		Init();
	}

	private void OnDestroy()
	{
		Clear();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
	}

	[Button]
	private void Init()
	{
		Clear();

		_boids = new List<Boid>(count);
		for (int i = 0; i < count; i++)
		{
			Boid boid = new Boid(
				new Vector3(
					Random.Range(bounds.min.x, bounds.max.x),
					Random.Range(bounds.min.y, bounds.max.y),
					Random.Range(bounds.min.z, bounds.max.z)),
				Random.onUnitSphere * Random.Range(speedRange.x, speedRange.y));

			_boids.Add(boid);
		}

		_boidTransforms = new Dictionary<Boid, Transform>(_boids.Count);
		foreach (Boid boid in _boids)
		{
			Transform newTransform = Instantiate(boidPrefab);
			_boidTransforms.Add(boid, newTransform);
		}

		_boidsData = _boids.Select(b => b.Info).ToNativeArray(Allocator.Persistent);
		_avoidResultAccelerations = new NativeArray<Vector3>(_boids.Count, Allocator.Persistent);
		_boundsResultAccelerations = new NativeArray<Vector3>(_boids.Count, Allocator.Persistent);
		_alignResultAccelerations = new NativeArray<Vector3>(_boids.Count, Allocator.Persistent);
		_cohesionResultAccelerations = new NativeArray<Vector3>(_boids.Count, Allocator.Persistent);
		_transformAccessArray = new TransformAccessArray(_boidTransforms.Values.ToArray(), jobCount);
	}

	private void Clear()
	{
		if (_boids == null)
		{
			return;
		}

		foreach (Transform transform in _boidTransforms.Values.ToArray())
		{
			Destroy(transform.gameObject);
		}

		_boids.Clear();
		_boidTransforms.Clear();

		_boidsData.Dispose();
		_avoidResultAccelerations.Dispose();
		_alignResultAccelerations.Dispose();
		_cohesionResultAccelerations.Dispose();
		_boundsResultAccelerations.Dispose();
		_transformAccessArray.Dispose();
	}

	private void Update()
	{
		JobHandle avoidHandle = ScheduleAvoidRuleJob();
		JobHandle boundsHandle = ScheduleBoundsRuleJob();
		JobHandle cohesionHandle = ScheduleCohesionRuleJob();
		JobHandle alignHandle = ScheduleAlignRuleJob();

		JobHandle combinedHandle = JobHandle.CombineDependencies(
			JobHandle.CombineDependencies(avoidHandle, boundsHandle),
			JobHandle.CombineDependencies(cohesionHandle, alignHandle));

		SetTransformsJob transformsJob = new SetTransformsJob(
			_boidsData,
			_alignResultAccelerations,
			_avoidResultAccelerations,
			_cohesionResultAccelerations,
			_boundsResultAccelerations,
			speedRange,
			Time.deltaTime);

		_jobHandle = transformsJob.Schedule(_transformAccessArray, combinedHandle);
	}

	private void LateUpdate()
	{
		_jobHandle.Complete();
	}

	private JobHandle ScheduleAlignRuleJob()
	{
		AlignJob alignJob = new AlignJob(
			_boidsData,
			_alignResultAccelerations,
			(LinearAnimationCurve) alignEffectByDistance);
		
		return alignJob.Schedule(_boidsData.Length, jobCount);
	}

	private JobHandle ScheduleCohesionRuleJob()
	{
		CohesionJob cohesionJob = new CohesionJob(
			_boidsData,
			_cohesionResultAccelerations,
			(LinearAnimationCurve) cohesionEffectByDistance);
		
		return cohesionJob.Schedule(_boidsData.Length, jobCount);
	}

	private JobHandle ScheduleAvoidRuleJob()
	{
		AvoidJob avoidJob = new AvoidJob(
			_boidsData,
			_avoidResultAccelerations,
			(LinearAnimationCurve) avoidEffectByDistance);
		
		return avoidJob.Schedule(_boidsData.Length, jobCount);
	}

	private JobHandle ScheduleBoundsRuleJob()
	{
		BoundsJob boundsJob = new BoundsJob(
			bounds,
			_boidsData,
			_boundsResultAccelerations,
			(LinearAnimationCurve) boundsEffectByDistance);
		
		return boundsJob.Schedule(_boidsData.Length, jobCount);
	}
}