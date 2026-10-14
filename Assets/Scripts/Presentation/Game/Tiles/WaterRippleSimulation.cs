using UnityEngine;

namespace Game.Tiles
{
	// Simulates ripples over a rectangle of water centred on this transform, so waves from WaterRippleSource objects
	// spread, bounce off the rectangle's edges and off other sources, and interfere. The Water shader reads the result.
	// An optional mask holds parts of the rectangle still, so ponds that aren't rectangles have walls of their own.
	public class WaterRippleSimulation : MonoBehaviour
	{
		private const float StepTime = 1 / 60f;
		private const int MaxStepsPerFrame = 4;

		private static readonly int TextureId = Shader.PropertyToID("_WaterRippleSimTex");
		private static readonly int RectId = Shader.PropertyToID("_WaterRippleSimRect");
		private static readonly int EnabledId = Shader.PropertyToID("_WaterRippleSimEnabled");
		private static readonly int SimRectId = Shader.PropertyToID("_SimRect");
		private static readonly int CourantId = Shader.PropertyToID("_Courant");
		private static readonly int DampingId = Shader.PropertyToID("_Damping");
		private static readonly int ForcePhaseId = Shader.PropertyToID("_ForcePhase");
		private static readonly int ForceWidthId = Shader.PropertyToID("_ForceWidth");
		private static readonly int SplashesId = Shader.PropertyToID("_Splashes");
		private static readonly int SplashCountId = Shader.PropertyToID("_SplashCount");
		private static readonly int MaskId = Shader.PropertyToID("_Mask");
		private const int MaxSplashesPerStep = 8;

		[SerializeField, Tooltip("World XZ size of the simulated area.")]
		private Vector2 size = new(5, 4);

		[SerializeField]
		private float texelsPerUnit = 40;

		[SerializeField, Tooltip("World units per second.")]
		private float waveSpeed = 0.25f;

		[SerializeField, Tooltip("Distance between rings emitted by sources, in world units.")]
		private float wavelength = 0.32f;

		[SerializeField, Tooltip("Fraction of wave height lost per second.")]
		private float dampingPerSecond = 0.15f;

		private Texture _mask;
		private Material _material;
		private RenderTexture _current;
		private RenderTexture _next;
		private float _accumulator;
		private float _time;
		private readonly Vector4[] _splashes = new Vector4[MaxSplashesPerStep];
		private int _splashCount;

		public Vector2 Size
		{
			get => size;
			set => size = value;
		}

		/// <summary>The simulation's texture size; a <see cref="Mask"/> should match it.</summary>
		public Vector2Int Resolution => new(Mathf.Max(2, Mathf.RoundToInt(size.x * texelsPerUnit)),
			Mathf.Max(2, Mathf.RoundToInt(size.y * texelsPerUnit)));

		/// <summary>
		/// Where its red channel is below 0.5 the water is held still, so ripples bounce off it like off the rectangle's
		/// edges. Covers the whole rectangle, point sampled. Null leaves the whole rectangle open.
		/// </summary>
		public Texture Mask
		{
			get => _mask;
			set => _mask = value;
		}

		private void Update()
		{
			_accumulator = Mathf.Min(_accumulator + Time.deltaTime, StepTime * MaxStepsPerFrame);
			while (_accumulator >= StepTime)
			{
				_accumulator -= StepTime;
				Step();
			}
		}

		private void OnDisable()
		{
			Shader.SetGlobalFloat(EnabledId, 0);
			Release(ref _current);
			Release(ref _next);
		}

		// Pushes the water down around a point once, e.g. where something lands in it; the ring spreads and bounces.
		// Splashes beyond MaxSplashesPerStep in one step are dropped.
		public void Splash(Vector3 position, float radius = 0.08f, float strength = 1)
		{
			if (_splashCount >= MaxSplashesPerStep)
			{
				return;
			}

			_splashes[_splashCount++] = new Vector4(position.x, position.z, Mathf.Max(radius, 0.001f), strength);
		}

		public void Simulate(float seconds)
		{
			int steps = Mathf.RoundToInt(seconds / StepTime);
			for (int i = 0; i < steps; i++)
			{
				Step();
			}
		}

		// Flattens the water, e.g. when the pond is rebuilt for a new level.
		public void Clear()
		{
			ClearTexture(_current);
			ClearTexture(_next);
			_splashCount = 0;
		}

		public void Step()
		{
			EnsureResources();
			WaterRippleSource.PushToShader();

			float texel = 1 / texelsPerUnit;
			float courant = waveSpeed * StepTime / texel;
			Vector3 centre = transform.position;
			var rect = new Vector4(centre.x - size.x / 2, centre.z - size.y / 2, size.x, size.y);
			_time += StepTime;

			_material.SetVector(SimRectId, rect);
			_material.SetFloat(CourantId, courant * courant);
			_material.SetFloat(DampingId, Mathf.Pow(1 - dampingPerSecond, StepTime));
			_material.SetFloat(ForcePhaseId, 2 * Mathf.PI * waveSpeed / wavelength * _time);
			_material.SetFloat(ForceWidthId, texel * 1.5f);
			_material.SetVectorArray(SplashesId, _splashes);
			_material.SetFloat(SplashCountId, _splashCount);
			_material.SetTexture(MaskId, _mask != null ? _mask : Texture2D.whiteTexture);
			_splashCount = 0;
			Graphics.Blit(_current, _next, _material);
			(_current, _next) = (_next, _current);

			Shader.SetGlobalTexture(TextureId, _current);
			Shader.SetGlobalVector(RectId, rect);
			Shader.SetGlobalFloat(EnabledId, 1);
		}

		private void EnsureResources()
		{
			if (_material == null)
			{
				_material = new Material(Shader.Find("Hidden/WaterRippleSimulation")) { hideFlags = HideFlags.DontSave };
			}

			Vector2Int resolution = Resolution;
			if (_current != null && _current.width == resolution.x && _current.height == resolution.y)
			{
				return;
			}

			Release(ref _current);
			Release(ref _next);
			_current = Create(resolution.x, resolution.y);
			_next = Create(resolution.x, resolution.y);
		}

		private static RenderTexture Create(int width, int height)
		{
			RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGFloat)
				? RenderTextureFormat.RGFloat
				: RenderTextureFormat.ARGBFloat;
			var texture = new RenderTexture(width, height, 0, format)
			{
				name = "WaterRippleSimulation",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.DontSave,
			};
			texture.Create();
			ClearTexture(texture);
			return texture;
		}

		private static void ClearTexture(RenderTexture texture)
		{
			if (texture == null)
			{
				return;
			}

			RenderTexture active = RenderTexture.active;
			RenderTexture.active = texture;
			GL.Clear(false, true, Color.clear);
			RenderTexture.active = active;
		}

		private static void Release(ref RenderTexture texture)
		{
			if (texture == null)
			{
				return;
			}

			texture.Release();
			if (Application.isPlaying)
			{
				Destroy(texture);
			}
			else
			{
				DestroyImmediate(texture);
			}

			texture = null;
		}
	}
}
