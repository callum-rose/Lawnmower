using UnityEngine;

namespace Game.Tiles
{
	// Bakes the Water shader's animated caustic web into a small tiling texture once a frame (WaterCausticBake.shader), so
	// water pixels sample it rather than each evaluating the web. Takes the web's speed from this object's Water material.
	public class WaterCausticBaker : MonoBehaviour
	{
		private const float Period = 8;

		private static readonly int TextureId = Shader.PropertyToID("_WaterCausticTex");
		private static readonly int GlobalPeriodId = Shader.PropertyToID("_WaterCausticPeriod");
		private static readonly int EnabledId = Shader.PropertyToID("_WaterCausticTexEnabled");
		private static readonly int PeriodId = Shader.PropertyToID("_Period");
		private static readonly int TimeScaleId = Shader.PropertyToID("_TimeScale");
		private static readonly int AnimationSpeedId = Shader.PropertyToID("_AnimationSpeed");
		private static readonly int CausticSpeedId = Shader.PropertyToID("_CausticSpeed");

		[SerializeField]
		private Shader shader;

		[SerializeField, Tooltip("Width and height of the baked texture, which holds 8 x 8 web cells.")]
		private int resolution = 512;

		private Material _material;
		private RenderTexture _texture;

		private void Update()
		{
			Bake();
		}

		private void OnDisable()
		{
			Shader.SetGlobalFloat(EnabledId, 0);
			if (_texture != null)
			{
				// Blit leaves its target active
				if (RenderTexture.active == _texture)
				{
					RenderTexture.active = null;
				}

				_texture.Release();
				DestroySafely(_texture);
				_texture = null;
			}

			if (_material != null)
			{
				DestroySafely(_material);
				_material = null;
			}
		}

		public void Bake()
		{
			if (!EnsureResources())
			{
				return;
			}

			float timeScale = 1;
			if (TryGetComponent(out Renderer waterRenderer) && waterRenderer.sharedMaterial != null)
			{
				Material water = waterRenderer.sharedMaterial;
				timeScale = water.GetFloat(AnimationSpeedId) * water.GetFloat(CausticSpeedId);
			}

			_material.SetFloat(PeriodId, Period);
			_material.SetFloat(TimeScaleId, timeScale);
			Graphics.Blit(null, _texture, _material);

			Shader.SetGlobalTexture(TextureId, _texture);
			Shader.SetGlobalFloat(GlobalPeriodId, Period);
			Shader.SetGlobalFloat(EnabledId, 1);
		}

		private bool EnsureResources()
		{
			if (_material == null)
			{
				Shader bakeShader = shader != null ? shader : Shader.Find("Hidden/WaterCausticBake");
				if (bakeShader == null)
				{
					Debug.LogError($"[{nameof(WaterCausticBaker)}] No bake shader; the water evaluates its caustics per pixel.");
					enabled = false;
					return false;
				}

				_material = new Material(bakeShader) { hideFlags = HideFlags.DontSave };
			}

			if (_texture == null)
			{
				RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf)
					? RenderTextureFormat.RGHalf
					: RenderTextureFormat.ARGBHalf;
				_texture = new RenderTexture(resolution, resolution, 0, format)
				{
					name = "WaterCaustics",
					wrapMode = TextureWrapMode.Repeat,
					filterMode = FilterMode.Bilinear,
					hideFlags = HideFlags.DontSave,
				};
			}

			return true;
		}

		private static void DestroySafely(Object obj)
		{
			if (Application.isPlaying)
			{
				Destroy(obj);
			}
			else
			{
				DestroyImmediate(obj);
			}
		}
	}
}
