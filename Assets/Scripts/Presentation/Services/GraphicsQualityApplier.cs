using System;
using Lawnmower.Presenters;
using Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lawnmower.Services
{
	/// <summary>
	/// Graphics Low/High: the Quality level, the URP render scale, and the grass/water shader quality keywords (through
	/// <see cref="GraphicsSettingState"/>). The URP asset is shared, so in the Editor the render scale and the Quality
	/// level are put back when play mode ends, to keep the project files clean.
	/// </summary>
	internal sealed class GraphicsQualityApplier
	{
		private readonly string _lowQualityLevel;
		private readonly string _highQualityLevel;
		private readonly float _lowRenderScale;
		private readonly float _highRenderScale;

		private float? _originalRenderScale;
		private int? _originalQualityLevel;

		public GraphicsQualityApplier(string lowQualityLevel, string highQualityLevel, float lowRenderScale, float highRenderScale)
		{
			_lowQualityLevel = lowQualityLevel;
			_highQualityLevel = highQualityLevel;
			_lowRenderScale = lowRenderScale;
			_highRenderScale = highRenderScale;
		}

		public static GraphicsSetting ToShaderSetting(GraphicsQuality quality) =>
			quality == GraphicsQuality.Low ? GraphicsSetting.Low : GraphicsSetting.High;

		public void Apply(GraphicsQuality quality)
		{
			bool isLow = quality == GraphicsQuality.Low;

			int level = Array.IndexOf(QualitySettings.names, isLow ? _lowQualityLevel : _highQualityLevel);

			if (level >= 0 && QualitySettings.GetQualityLevel() != level)
			{
				_originalQualityLevel ??= QualitySettings.GetQualityLevel();
				QualitySettings.SetQualityLevel(level, true);
			}

			if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
			{
				_originalRenderScale ??= urp.renderScale;
				urp.renderScale = isLow ? _lowRenderScale : _highRenderScale;
			}

			GraphicsSettingState.Current = ToShaderSetting(quality);
		}

		/// <summary>Undoes <see cref="Apply"/>'s changes to shared assets (Editor play mode ending).</summary>
		public void Restore()
		{
			if (_originalQualityLevel.HasValue)
			{
				QualitySettings.SetQualityLevel(_originalQualityLevel.Value, true);
				_originalQualityLevel = null;
			}

			if (_originalRenderScale.HasValue && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
			{
				urp.renderScale = _originalRenderScale.Value;
				_originalRenderScale = null;
			}

			GraphicsSettingState.Current = GraphicsSettingState.Default;
		}
	}
}
