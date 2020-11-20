using UnityEngine;

namespace Game.Tiles
{
    internal class AppearanceSetter : MonoBehaviour, IAppearanceSetter
    {
        [SerializeField] private GameObject largeContainer, mediumContainer, smallContainer, ruinedContainer;

        public void SetAppearance(GrassTile tile)
        {
            largeContainer.SetActive(tile.GrassHeight == 3);
            mediumContainer.SetActive(tile.GrassHeight == 2);
            smallContainer.SetActive(tile.GrassHeight == 1);
            ruinedContainer.SetActive(tile.GrassHeight == 0);
        }
    }
}
