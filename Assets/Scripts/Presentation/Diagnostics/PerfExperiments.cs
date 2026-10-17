#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Tiles;
using Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Lawnmower.Diagnostics
{
	/// <summary>
	/// Development builds only, driven by launch environment variables:
	/// PERF_LEVEL=n opens level n from the main menu once; in the Game scene, PERF_SWEEP=1 then turns rendering features
	/// off one at a time, PERF_SWEEP=shaders stops drawing each shader in turn and PERF_SWEEP=water turns the water
	/// shader's features off one at a time and PERF_SWEEP=scale tries render scales, tagging the PerfStats reports for each.
	/// </summary>
	public class PerfExperiments : MonoBehaviour
	{
		private const float SettleSeconds = 1.5f;
		private const float HoldSeconds = 10.5f;

		private static bool autoLevelTaken;
		private static string sweep;

		private UniversalRenderPipelineAsset urp;
		private float baseRenderScale;
		private float baseShadowDistance;
		private bool baseHdr;
		private bool baseDepthTexture;
		private bool baseOpaqueTexture;
		private UpscalingFilterSelection baseUpscaling;
		private GraphicsSetting baseShaders;
		private bool? cameraPost;
		private CameraOverrideOption? cameraDepth;
		private CameraOverrideOption? cameraColor;
		private readonly Dictionary<string, float> savedFloats = new Dictionary<string, float>();

		public static bool TryTakeAutoLevel(out int index)
		{
			index = -1;
			if (autoLevelTaken || !int.TryParse(Environment.GetEnvironmentVariable("PERF_LEVEL"), out index))
			{
				return false;
			}

			autoLevelTaken = true;
			return true;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Create()
		{
			sweep = Environment.GetEnvironmentVariable("PERF_SWEEP");
			if (sweep != "1" && sweep != "shaders" && sweep != "water" && sweep != "scale")
			{
				return;
			}

			GameObject go = new GameObject(nameof(PerfExperiments));
			DontDestroyOnLoad(go);
			go.AddComponent<PerfExperiments>();
		}

		private IEnumerator Start()
		{
			while (SceneManager.GetActiveScene().name != "Game" || Camera.main == null)
			{
				yield return null;
			}

			yield return new WaitForSeconds(3f);

			urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
			if (urp == null)
			{
				Debug.LogError("[PerfExp] No URP asset; sweep skipped.");
				yield break;
			}

			baseRenderScale = urp.renderScale;
			baseShadowDistance = urp.shadowDistance;
			baseHdr = urp.supportsHDR;
			baseDepthTexture = urp.supportsCameraDepthTexture;
			baseOpaqueTexture = urp.supportsCameraOpaqueTexture;
			baseUpscaling = urp.upscalingFilter;
			baseShaders = GraphicsSettingState.Current;
			Debug.Log($"[PerfExp] base renderScale={baseRenderScale} shadowDistance={baseShadowDistance} hdr={baseHdr} " +
				$"depthTex={baseDepthTexture} opaqueTex={baseOpaqueTexture} shaders={baseShaders} msaa={urp.msaaSampleCount} " +
				$"quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} reflections={FindObjectsByType<PlanarWaterReflection>(FindObjectsSortMode.None).Length}");

			if (sweep == "scale")
			{
				yield return Run("scale100", () => urp.renderScale = 1f);
				foreach (float scale in new[] { 0.9f, 0.85f, 0.8f, 0.75f })
				{
					yield return Run("scale" + Mathf.RoundToInt(scale * 100), () => urp.renderScale = scale);
				}

				yield return Run("scale85fsr", () =>
				{
					urp.renderScale = 0.85f;
					urp.upscalingFilter = UpscalingFilterSelection.FSR;
				});
				yield return Run("scale100again", () => urp.renderScale = 1f);
				Debug.Log("[PerfExp] done");
				yield break;
			}

			if (sweep == "shaders" || sweep == "water")
			{
				yield return sweep == "shaders" ? ShaderSweep() : WaterSweep();
				Debug.Log("[PerfExp] done");
				yield break;
			}

			yield return Run("baseline", () => { });
			yield return Run("scale75", () => urp.renderScale = 0.75f);
			yield return Run("scale50", () => urp.renderScale = 0.5f);
			yield return Run("noReflection", () => SetReflections(false));
			yield return Run("noShadows", () => urp.shadowDistance = 0f);
			yield return Run("noPost", () => Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = false);
			yield return Run("noDepthOpaque", SetNoDepthOpaque);
			yield return Run("noHDR", () => urp.supportsHDR = false);
			yield return Run("lowShaders", () => GraphicsSettingState.Current = GraphicsSetting.Low);
			yield return Run("allOff", () =>
			{
				urp.renderScale = 0.5f;
				SetReflections(false);
				urp.shadowDistance = 0f;
				Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = false;
				SetNoDepthOpaque();
				urp.supportsHDR = false;
				GraphicsSettingState.Current = GraphicsSetting.Low;
			});
			yield return Run("baselineAgain", () => { });

			Debug.Log("[PerfExp] done");
		}

		private IEnumerator ShaderSweep()
		{
			Dictionary<string, HashSet<Material>> materialsByShader = new Dictionary<string, HashSet<Material>>();
			Dictionary<string, List<Renderer>> renderersByShader = new Dictionary<string, List<Renderer>>();

			foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (!renderer.enabled)
				{
					continue;
				}

				foreach (Material material in renderer.sharedMaterials)
				{
					if (material == null)
					{
						continue;
					}

					GetOrAdd(materialsByShader, material.shader.name).Add(material);
					List<Renderer> renderers = GetOrAdd(renderersByShader, material.shader.name);
					if (!renderers.Contains(renderer))
					{
						renderers.Add(renderer);
					}
				}
			}

			foreach (Material material in InstancedTileRenderer.DebugMaterials)
			{
				if (material != null)
				{
					GetOrAdd(materialsByShader, material.shader.name).Add(material);
				}
			}

			foreach (KeyValuePair<string, HashSet<Material>> group in materialsByShader)
			{
				renderersByShader.TryGetValue(group.Key, out List<Renderer> renderers);
				Debug.Log($"[PerfExp] shader '{group.Key}': renderers={renderers?.Count ?? 0} " +
					$"materials={string.Join(",", group.Value.Select(m => m.name))}");
			}

			yield return Run("baseline", () => { });

			foreach (KeyValuePair<string, HashSet<Material>> group in materialsByShader)
			{
				HashSet<Material> materials = group.Value;
				renderersByShader.TryGetValue(group.Key, out List<Renderer> renderers);
				yield return Run("off:" + group.Key.Replace(' ', '_'),
					() => SetShaderDrawn(materials, renderers, false),
					() => SetShaderDrawn(materials, renderers, true));
			}

			yield return Run("baselineAgain", () => { });
		}

		private IEnumerator WaterSweep()
		{
			List<Renderer> water = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
				.Where(r => r.enabled && r.sharedMaterial != null && r.sharedMaterial.shader.name == "Water").ToList();
			if (water.Count == 0)
			{
				Debug.LogError("[PerfExp] No water in this level; sweep skipped.");
				yield break;
			}

			Material material = water[0].sharedMaterial;
			WaterRippleSimulation[] ripples = FindObjectsByType<WaterRippleSimulation>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			Debug.Log($"[PerfExp] water renderers={water.Count} ripples={ripples.Length} " +
				$"bounds={water[0].bounds.size} depthBlur={material.GetFloat("_DepthBlur")} " +
				$"web={material.GetFloat("_WebCausticAmount")} split={material.GetFloat("_CausticSplit")}");

			Action noDepthBlur = SetFloat(material, "_DepthBlur", 0);
			Action noWeb = SetFloat(material, "_WebCausticAmount", 0);
			Action noSplit = SetFloat(material, "_CausticSplit", 0);
			Action noRipples = () => SetEnabled(ripples, false);
			Action noReflection = () => SetReflections(false);

			yield return Run("baseline", () => { });
			yield return Run("waterHidden", () => water.ForEach(r => r.enabled = false), () => water.ForEach(r => r.enabled = true));
			WaterCausticBaker[] bakers = FindObjectsByType<WaterCausticBaker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			yield return Run("perPixelWeb", () => SetEnabled(bakers, false), () => SetEnabled(bakers, true));
			yield return Run("noDepthBlur", noDepthBlur, Restore(material));
			yield return Run("noWebCaustics", noWeb, Restore(material));
			yield return Run("webNoSplit", noSplit, Restore(material));
			yield return Run("noRippleSim", noRipples, () => SetEnabled(ripples, true));
			yield return Run("allWaterFeaturesOff", () =>
			{
				noDepthBlur();
				noWeb();
				noRipples();
				noReflection();
			}, () =>
			{
				Restore(material)();
				SetEnabled(ripples, true);
			});
			yield return Run("baselineAgain", () => { });
		}

		private Action SetFloat(Material material, string property, float value)
		{
			savedFloats[property] = material.GetFloat(property);
			return () => material.SetFloat(property, value);
		}

		private Action Restore(Material material)
		{
			return () =>
			{
				foreach (KeyValuePair<string, float> saved in savedFloats)
				{
					material.SetFloat(saved.Key, saved.Value);
				}
			};
		}

		private static void SetEnabled(IEnumerable<Behaviour> behaviours, bool enabled)
		{
			foreach (Behaviour behaviour in behaviours)
			{
				behaviour.enabled = enabled;
			}
		}

		private static void SetShaderDrawn(HashSet<Material> materials, List<Renderer> renderers, bool drawn)
		{
			InstancedTileRenderer.DebugSkipMaterial = drawn ? null : materials.Contains;
			if (renderers == null)
			{
				return;
			}

			foreach (Renderer renderer in renderers)
			{
				if (renderer != null)
				{
					renderer.enabled = drawn;
				}
			}
		}

		private static TValue GetOrAdd<TValue>(Dictionary<string, TValue> dictionary, string key) where TValue : new()
		{
			if (!dictionary.TryGetValue(key, out TValue value))
			{
				value = new TValue();
				dictionary.Add(key, value);
			}

			return value;
		}

		private IEnumerator Run(string tag, Action apply, Action undo = null)
		{
			Restore();
			yield return null;
			apply();
			yield return new WaitForSeconds(SettleSeconds);
			PerfStats.Mark(tag);
			yield return new WaitForSeconds(HoldSeconds);
			undo?.Invoke();
		}

		private void Restore()
		{
			urp.renderScale = baseRenderScale;
			urp.shadowDistance = baseShadowDistance;
			urp.supportsHDR = baseHdr;
			urp.supportsCameraDepthTexture = baseDepthTexture;
			urp.supportsCameraOpaqueTexture = baseOpaqueTexture;
			urp.upscalingFilter = baseUpscaling;
			GraphicsSettingState.Current = baseShaders;
			SetReflections(true);

			UniversalAdditionalCameraData data = Camera.main.GetUniversalAdditionalCameraData();
			data.renderPostProcessing = cameraPost ??= data.renderPostProcessing;
			data.requiresDepthOption = cameraDepth ??= data.requiresDepthOption;
			data.requiresColorOption = cameraColor ??= data.requiresColorOption;
		}

		private void SetNoDepthOpaque()
		{
			urp.supportsCameraDepthTexture = false;
			urp.supportsCameraOpaqueTexture = false;
			UniversalAdditionalCameraData data = Camera.main.GetUniversalAdditionalCameraData();
			data.requiresDepthOption = CameraOverrideOption.Off;
			data.requiresColorOption = CameraOverrideOption.Off;
		}

		private static void SetReflections(bool enabled)
		{
			foreach (PlanarWaterReflection reflection in FindObjectsByType<PlanarWaterReflection>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				reflection.enabled = enabled;
			}
		}
	}
}
#endif
