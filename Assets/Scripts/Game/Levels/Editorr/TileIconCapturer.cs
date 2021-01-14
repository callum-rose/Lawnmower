using BalsamicBits.Extensions;
using Core;
using Game.Tiles;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Levels.Editorr
{
    internal class TileIconCapturer : MonoBehaviour
    {
        [SerializeField] private int textureSize = 100;
        [SerializeField] private UnityEngine.Camera camera;
        [FormerlySerializedAs("tilePrefabHolder")] [SerializeField] private TilePrefabsManager tilePrefabManager;

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

        public RenderTexture Setup(Tile data)
        {
            BaseTileObject newTileObject = tilePrefabManager.GetPrefabAndInstantiate(data);
            newTileObject.Bind(data);
            
            newTileObject.transform.localPosition = Vector3.zero;
            newTileObject.gameObject.SetLayerRecursively((int)UnityLayers.UiTile);

            _renderTexture = new RenderTexture(textureSize, textureSize, 1);
            camera.targetTexture = _renderTexture;
            _renderTexture.name = data.GetType().ToString();

            return _renderTexture;
        }

        public void Render()
        {
            camera.Render();
        }

        #endregion
    }
}