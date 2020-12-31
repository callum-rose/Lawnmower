using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[ExecuteInEditMode]
	internal class GrassTileGrassMaterialSetter : MonoBehaviour
	{
		[SerializeField] private bool enabled = true;

		[SerializeField, InlineEditor, ShowIf(nameof(enabled))]
		private GrassMaterialDataHolder materialData;

		[SerializeField, ShowIf(nameof(enabled))]
		private int height;

		[ShowInInspector, ShowIf(nameof(enabled))]
		private GrassMaterialDataHolder.GrassData DataToUse => materialData.GetDataForHeight(height);

		private static MaterialPropertyBlock _propertyBlock;

		private static readonly int 
			ColourFadeBaseColour = Shader.PropertyToID("_ColourFadeBaseColour"),
			ColourFadeTipColour = Shader.PropertyToID("_ColourFadeTipColour"),
			ColourFadeYRange = Shader.PropertyToID("_ColourFadeYRange");

		private Renderer[] _renderers;

		#region Unity

		private void Awake()
		{
			if (!enabled)
			{
				return;
			}

			_propertyBlock ??= new MaterialPropertyBlock();

			UpdateRendererArray();
			SetPropertyBlock();
		}

		private void OnValidate()
		{
			if (!enabled)
			{
				return;
			}

			SetPropertyBlock();
		}

		#endregion

		#region Methods

		[Button, EnableIf(nameof(enabled))]
		private void SetPropertyBlock()
		{
#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying)
			{
				_propertyBlock ??= new MaterialPropertyBlock();
				_renderers ??= GetComponentsInChildren<Renderer>();
			}
#endif

			GrassMaterialDataHolder.GrassData data = materialData.GetDataForHeight(height);
			foreach (Renderer r in _renderers)
			{
				r.GetPropertyBlock(_propertyBlock);
				_propertyBlock.SetColor(ColourFadeBaseColour, data.BaseColour);
				_propertyBlock.SetColor(ColourFadeTipColour, materialData.VaryColour(data.TipColour));
				_propertyBlock.SetVector(ColourFadeYRange, data.ColourFadeYRange);
				r.SetPropertyBlock(_propertyBlock);
			}
		}

		[Button, EnableIf(nameof(enabled))]
		private void UpdateRendererArray()
		{
			_renderers = GetComponentsInChildren<Renderer>();
		}

		#endregion
	}
}