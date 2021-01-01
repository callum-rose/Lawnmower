using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LevelSelect
{
    internal class LevelTile : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private GameObject padlock;

        public event Action Clicked;

        private int _number;

        public void Setup(int number, bool isLocked)
        {
            _number = number;
            numberText.text = _number.ToString();
            
            padlock.SetActive(isLocked);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked.Invoke();
        }
    }
}