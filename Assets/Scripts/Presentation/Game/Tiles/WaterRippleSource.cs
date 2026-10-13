using System.Collections.Generic;
using UnityEngine;

namespace Game.Tiles
{
	// Something touching the water surface (a rock poking through, a lily pad, a fish). Its footprint blocks ripples so
	// waves bounce off it, and with strength > 0 it also sends out rings. Read by WaterRippleSimulation.
	[ExecuteAlways]
	public class WaterRippleSource : MonoBehaviour
	{
		private const int MaxSources = 16;

		private static readonly int SourcesId = Shader.PropertyToID("_WaterRipples");
		private static readonly int CountId = Shader.PropertyToID("_WaterRippleCount");
		private static readonly List<WaterRippleSource> Sources = new();
		private static readonly Vector4[] Packed = new Vector4[MaxSources];
		private static int _lastPushedFrame = -1;

		[SerializeField, Tooltip("Radius of the object where it meets the water; rings start here.")]
		private float radius = 0.3f;

		[SerializeField, Range(0, 2), Tooltip("How strongly it sends out rings; 0 only blocks ripples.")]
		private float strength = 0.2f;

		public float Radius
		{
			get => radius;
			set => radius = value;
		}

		public float Strength
		{
			get => strength;
			set => strength = value;
		}

		private void OnEnable()
		{
			Sources.Add(this);
		}

		private void OnDisable()
		{
			Sources.Remove(this);
			PushToShader();
		}

		private void LateUpdate()
		{
			if (_lastPushedFrame == Time.frameCount)
			{
				return;
			}

			_lastPushedFrame = Time.frameCount;
			PushToShader();
		}

		public static void PushToShader()
		{
			int count = Mathf.Min(Sources.Count, MaxSources);
			for (int i = 0; i < count; i++)
			{
				Vector3 position = Sources[i].transform.position;
				Packed[i] = new Vector4(position.x, position.z, Sources[i].radius, Sources[i].strength);
			}

			Shader.SetGlobalVectorArray(SourcesId, Packed);
			Shader.SetGlobalFloat(CountId, count);
		}
	}
}
