using BalsamicBits.Extensions;
using Core;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels.Editorr
{
    public class TileIconCapturer : MonoBehaviour
    {
        [SerializeField] private int textureSize = 100;
        [SerializeField] private UnityEngine.Camera camera;
        [SerializeField] private TilePrefabsHolder tilePrefabHolder;

        private RenderTexture _renderTexture;

        #region Unity

        private void Start()
        {
            camera.enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(_renderTexture);
        }

        #endregion

        #region API

        public RenderTexture Setup(TileData data)
        {
            Tile prefab = tilePrefabHolder.GetPrefab(data.Type);

            Tile newTile = Instantiate(prefab, transform);
            newTile.Setup(data.Data);
            newTile.transform.localPosition = Vector3.zero;
            newTile.gameObject.SetLayerRecursively((int)UnityLayers.UiTile);

            _renderTexture = new RenderTexture(textureSize, textureSize, 1);
            camera.targetTexture = _renderTexture;
            _renderTexture.name = data.Type.ToString();

            return _renderTexture;
        }

        public void Render()
        {
            camera.Render();
        }

        #endregion
    }
}