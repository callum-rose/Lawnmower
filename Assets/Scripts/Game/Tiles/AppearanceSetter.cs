using UnityEngine;

namespace Game.Tiles
{
    internal class AppearanceSetter : MonoBehaviour, IAppearanceSetter
    {
        [SerializeField] private GameObject largeContainer, mediumContainer, smallContainer, ruinedContainer;

        public void SetAppearance(int grassHeight)
        {
            largeContainer.SetActive(grassHeight == 3);
            mediumContainer.SetActive(grassHeight == 2);
            smallContainer.SetActive(grassHeight== 1);
            ruinedContainer.SetActive(grassHeight == 0);
        }
    }
}
