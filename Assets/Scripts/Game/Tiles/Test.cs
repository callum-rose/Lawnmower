using UnityEngine;

namespace Game.Tiles
{
    [RequireComponent(typeof(Renderer)), ExecuteInEditMode]
    public class Test : MonoBehaviour
    {
        [SerializeField] private Vector3 test;
        [SerializeField] private float curveMaxSize;

        private static MaterialPropertyBlock _propertyBlock;

        private Renderer _renderer;

        private void Awake()
        {
            OnValidate();

            transform.localScale = Vector3.one * (10 * Random.Range(0.9f, 1.1f));
        }

        private void OnValidate()
        {
            Vector2 circle = Random.insideUnitCircle;
            test = new Vector3(circle.x, 0, circle.y) * curveMaxSize;

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector("_Offset", test);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
