using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LevelSelect
{
    internal class LevelTile : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI numberText;

        public event Action<int> Clicked;

        private int _number;

        public void SetNumber(int number)
        {
            _number = number;
            numberText.text = _number.ToString();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked.Invoke(_number);
        }
    }
}