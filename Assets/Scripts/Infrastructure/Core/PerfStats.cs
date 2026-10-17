#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace Core
{
	/// <summary>
	/// Development builds only: logs frame rate and memory every few seconds as a "[Perf]" line
	/// and shows the latest numbers in a corner overlay.
	/// </summary>
	public class PerfStats : MonoBehaviour
	{
		private const float ReportInterval = 5f;
		private const int MaxSamples = 1024;
		private const float LongFrameMs = 1000f / 55f;
		private const double Mb = 1024 * 1024;

		private readonly float[] frameMs = new float[MaxSamples];
		private readonly StringBuilder builder = new StringBuilder(256);

		private ProfilerRecorder systemMemory;
		private ProfilerRecorder totalMemory;
		private ProfilerRecorder gcMemory;
		private ProfilerRecorder gfxMemory;
		private ProfilerRecorder batches;
		private ProfilerRecorder setPassCalls;
		private ProfilerRecorder triangles;

		private readonly FrameTiming[] timings = new FrameTiming[1];
		private double cpuMainMs;
		private double cpuRenderMs;
		private double gpuMs;
		private int timingCount;

		private int sampleCount;
		private float elapsed;
		private long peakSystemBytes;
		private string overlay = "[Perf] measuring...";
		private GUIStyle style;

		private static PerfStats instance;
		private static string tag = "";

		/// <summary>Labels the following reports and starts a fresh measurement window.</summary>
		public static void Mark(string newTag)
		{
			tag = newTag;
			if (instance != null)
			{
				instance.ResetWindow();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Create()
		{
			GameObject go = new GameObject(nameof(PerfStats));
			DontDestroyOnLoad(go);
			instance = go.AddComponent<PerfStats>();
		}

		private void OnEnable()
		{
			systemMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
			totalMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
			gcMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
			gfxMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Gfx Used Memory");
			batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
			setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
			triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
		}

		private void OnDisable()
		{
			systemMemory.Dispose();
			totalMemory.Dispose();
			gcMemory.Dispose();
			gfxMemory.Dispose();
			batches.Dispose();
			setPassCalls.Dispose();
			triangles.Dispose();
		}

		private void Update()
		{
			float dt = Time.unscaledDeltaTime;
			if (sampleCount < MaxSamples)
			{
				frameMs[sampleCount++] = dt * 1000f;
			}

			FrameTimingManager.CaptureFrameTimings();
			if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
			{
				cpuMainMs += timings[0].cpuMainThreadFrameTime;
				cpuRenderMs += timings[0].cpuRenderThreadFrameTime;
				gpuMs += timings[0].gpuFrameTime;
				timingCount++;
			}

			elapsed += dt;
			peakSystemBytes = Math.Max(peakSystemBytes, systemMemory.LastValue);

			if (elapsed >= ReportInterval)
			{
				Report();
				ResetWindow();
			}
		}

		private void ResetWindow()
		{
			sampleCount = 0;
			elapsed = 0f;
			cpuMainMs = cpuRenderMs = gpuMs = 0;
			timingCount = 0;
		}

		private void Report()
		{
			if (sampleCount == 0)
			{
				return;
			}

			float total = 0f;
			int longFrames = 0;
			for (int i = 0; i < sampleCount; i++)
			{
				total += frameMs[i];
				if (frameMs[i] > LongFrameMs)
				{
					longFrames++;
				}
			}

			Array.Sort(frameMs, 0, sampleCount);
			float worstMs = frameMs[sampleCount - 1];
			float p99Ms = frameMs[Mathf.Min(sampleCount - 1, (int)(sampleCount * 0.99f))];
			float avgFps = sampleCount * 1000f / total;

			builder.Clear();
			builder.Append("[Perf] ");
			if (tag.Length > 0)
			{
				builder.Append("tag=").Append(tag).Append(' ');
			}

			builder.Append("scene=").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
				.Append(" fps=").Append(avgFps.ToString("F1"))
				.Append(" p99ms=").Append(p99Ms.ToString("F1"))
				.Append(" maxms=").Append(worstMs.ToString("F1"))
				.Append(" slow=").Append(longFrames).Append('/').Append(sampleCount);
			if (timingCount > 0)
			{
				builder.Append(" cpuMain=").Append((cpuMainMs / timingCount).ToString("F1"))
					.Append(" cpuRender=").Append((cpuRenderMs / timingCount).ToString("F1"))
					.Append(" gpu=").Append((gpuMs / timingCount).ToString("F1"));
			}

			builder.Append(" batches=").Append(batches.LastValue)
				.Append(" setPass=").Append(setPassCalls.LastValue)
				.Append(" tris=").Append(triangles.LastValue)
				.Append(" res=").Append(Screen.width).Append('x').Append(Screen.height)
				.Append(" | sysMB=").Append((systemMemory.LastValue / Mb).ToString("F0"))
				.Append(" peakSysMB=").Append((peakSystemBytes / Mb).ToString("F0"))
				.Append(" totalMB=").Append((totalMemory.LastValue / Mb).ToString("F0"))
				.Append(" gcMB=").Append((gcMemory.LastValue / Mb).ToString("F0"))
				.Append(" gfxMB=").Append((gfxMemory.LastValue / Mb).ToString("F0"));

			overlay = builder.ToString();
			Debug.Log(overlay);
		}

		private void OnGUI()
		{
			style ??= new GUIStyle(GUI.skin.label) { fontSize = Screen.height / 70, wordWrap = true };
			float topInset = Screen.height - Screen.safeArea.yMax;
			GUI.Label(new Rect(10, topInset + 10,Screen.width - 20, Screen.height / 10f), overlay, style);
		}
	}
}
#endif
