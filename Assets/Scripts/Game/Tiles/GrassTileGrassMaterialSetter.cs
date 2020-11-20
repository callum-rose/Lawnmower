using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
    [ExecuteInEditMode]
    internal class GrassTileGrassMaterialSetter : MonoBehaviour
    {
        [SerializeField] private bool enabled = true;

        [SerializeField, InlineEditor, ShowIf(nameof(enabled))] private GrassMaterialDataHolder materialData;
        [SerializeField, ShowIf(nameof(enabled))] private int height;

        [ShowInInspector, ShowIf(nameof(enabled))] private GrassMaterialDataHolder.GrassData DataToUse => materialData.GetDataForHeight(height);

        private static MaterialPropertyBlock _propertyBlock;

        private Renderer[] _renderers;

        #region Unity

        private void Awake()
        {
            if (!enabled)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

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
                if (_propertyBlock == null)
                {
                    _propertyBlock = new MaterialPropertyBlock();
                }

                if (_renderers == null)
                {
                    _renderers = GetComponentsInChildren<Renderer>();
                }
            }
#endif

            var data = materialData.GetDataForHeight(height);
            foreach (Renderer r in _renderers)
            {
                r.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_ColourFadeBaseColour", data.BaseColour);
                _propertyBlock.SetColor("_ColourFadeTipColour", materialData.VaryColour(data.TipColour));
                _propertyBlock.SetVector("_ColourFadeYRange", data.ColourFadeYRange);
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
