using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Tiles
{
    internal class AppearanceSetter : MonoBehaviour, IAppearanceSetter
    {
        // Mirror TILES in Assets/Models/Tile/Grass/Blender/generate_thin_grass.py, indexed by grass height
        private static readonly float[] GrassMeshHeights = { 0, 0.4f, 0.5f, 0.6f };
        private static readonly int FadeId = Shader.PropertyToID("_Fade");

        // A zero scale has no inverse, which breaks the normals
        private const float MinScale = 0.01f;

        [SerializeField] private GameObject largeContainer, mediumContainer, smallContainer, ruinedContainer;

        [Header("Transition")]
        [SerializeField, Min(0)] private float transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve transitionEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private readonly List<MeshRenderer> _rendererBuffer = new List<MeshRenderer>();
        private MaterialPropertyBlock _propertyBlock;
        private Transition _transition;
        private Coroutine _transitionRoutine;

        public void SetAppearance(int grassHeight)
        {
            EndTransition();
            Show(grassHeight);
        }

        public void TransitionAppearance(int fromGrassHeight, int toGrassHeight)
        {
            EndTransition();

            if (fromGrassHeight == toGrassHeight || transitionDuration <= 0 || !isActiveAndEnabled)
            {
                Show(toGrassHeight);
                return;
            }

            BeginTransition(fromGrassHeight, toGrassHeight);
            _transitionRoutine = StartCoroutine(TransitionRoutine());
        }

        private void OnDisable()
        {
            EndTransition();
        }

        private IEnumerator TransitionRoutine()
        {
            for (float time = 0; time < transitionDuration; time += Time.deltaTime)
            {
                ApplyTransition(transitionEase.Evaluate(time / transitionDuration));
                yield return null;
            }

            _transitionRoutine = null;
            EndTransition();
        }

        // Crossfade: both grasses scale together from the old grass's height to the new one's, so they always match.
        // The new grass fades in over the first half, then the old grass fades out over the second, so the tile never
        // looks see-through partway.
        private void BeginTransition(int fromGrassHeight, int toGrassHeight)
        {
            float fromHeight = GrassMeshHeights[fromGrassHeight];
            float toHeight = GrassMeshHeights[toGrassHeight];

            Show(fromGrassHeight);
            GetContainer(toGrassHeight).SetActive(true);

            _transition = new Transition
            {
                FinalGrassHeight = toGrassHeight,
                Outgoing = CreateFade(fromGrassHeight, 1, toHeight / fromHeight),
                Incoming = CreateFade(toGrassHeight, fromHeight / toHeight, 1),
            };

            ApplyTransition(0);

            Fade CreateFade(int grassHeight, float fromScale, float toScale)
            {
                if (grassHeight == 0)
                {
                    // The dirt has no grass to scale or fade; it sits under the grass for the whole transition
                    return null;
                }

                Fade fade = new Fade
                {
                    Container = GetContainer(grassHeight).transform,
                    FromScale = Mathf.Max(MinScale, fromScale),
                    ToScale = Mathf.Max(MinScale, toScale),
                };

                // Switched-on renderers draw themselves rather than through InstancedTileRenderer, so they can take the
                // property block; their original state is put back at the end
                fade.Container.GetComponentsInChildren(_rendererBuffer);
                foreach (MeshRenderer meshRenderer in _rendererBuffer)
                {
                    if (meshRenderer.sharedMaterial && meshRenderer.sharedMaterial.HasProperty(FadeId))
                    {
                        fade.Renderers.Add((meshRenderer, meshRenderer.enabled, meshRenderer.sortingOrder));
                        meshRenderer.enabled = true;
                    }
                }

                _rendererBuffer.Clear();
                return fade;
            }
        }

        private void ApplyTransition(float t)
        {
            Apply(_transition?.Incoming, t, Mathf.Clamp01(t * 2));
            Apply(_transition?.Outgoing, t, Mathf.Clamp01(2 - t * 2));

            void Apply(Fade fade, float scaleT, float alpha)
            {
                if (fade == null)
                {
                    return;
                }

                fade.Container.localScale = new Vector3(1, Mathf.LerpUnclamped(fade.FromScale, fade.ToScale, scaleT), 1);

                // Created here: Unity doesn't allow it in a field initialiser
                _propertyBlock ??= new MaterialPropertyBlock();
                _propertyBlock.SetFloat(FadeId, alpha);
                // The fading grass draws after the solid one: it still writes depth, so drawn first it would hide the
                // solid grass's blades behind its own faint ones
                foreach ((MeshRenderer meshRenderer, bool _, int sortingOrder) in fade.Renderers)
                {
                    meshRenderer.SetPropertyBlock(_propertyBlock);
                    meshRenderer.sortingOrder = sortingOrder + (alpha < 1 ? 1 : 0);
                }
            }
        }

        private void EndTransition()
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }

            if (_transition == null)
            {
                return;
            }

            Restore(_transition.Outgoing);
            Restore(_transition.Incoming);
            Show(_transition.FinalGrassHeight);
            _transition = null;

            static void Restore(Fade fade)
            {
                if (fade == null)
                {
                    return;
                }

                fade.Container.localScale = Vector3.one;
                foreach ((MeshRenderer meshRenderer, bool wasEnabled, int sortingOrder) in fade.Renderers)
                {
                    if (meshRenderer)
                    {
                        meshRenderer.SetPropertyBlock(null);
                        meshRenderer.enabled = wasEnabled;
                        meshRenderer.sortingOrder = sortingOrder;
                    }
                }
            }
        }

        private void Show(int grassHeight)
        {
            largeContainer.SetActive(grassHeight == 3);
            mediumContainer.SetActive(grassHeight == 2);
            smallContainer.SetActive(grassHeight == 1);
            ruinedContainer.SetActive(grassHeight == 0);
        }

        private GameObject GetContainer(int grassHeight)
        {
            switch (grassHeight)
            {
                case 3: return largeContainer;
                case 2: return mediumContainer;
                case 1: return smallContainer;
                default: return ruinedContainer;
            }
        }

        private class Transition
        {
            public int FinalGrassHeight;
            public Fade Outgoing, Incoming;
        }

        private class Fade
        {
            public Transform Container;
            public float FromScale, ToScale;
            public readonly List<(MeshRenderer renderer, bool wasEnabled, int sortingOrder)> Renderers =
                new List<(MeshRenderer, bool, int)>();
        }
    }
}
