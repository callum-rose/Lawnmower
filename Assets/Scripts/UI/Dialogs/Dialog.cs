using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using System;
using UI.Buttons;

namespace UI.Dialogs
{
    internal class Dialog : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField, AssetsOnly] private Button buttonPrefab;
        [SerializeField] private Transform buttonContainer;

        public event Action<Dialog> Hidden;

        public int Id { get; private set; }

        public void Init(int id, string header, string body, ButtonInfo buttonInfo)
        {
            Id = id;

            SetText(header, body);

            CreateButton(buttonInfo);
        }

        public void Init(int id, string header, string body, ButtonInfo leftButtonInfo, ButtonInfo rightButtonInfo)
        {
            Id = id;

            SetText(header, body);

            CreateButton(leftButtonInfo);
            CreateButton(rightButtonInfo);
        }

        public void Show()
        {

        }

        public void Close()
        {
            Hidden.Invoke(this);
        }

        private void SetText(string header, string body)
        {
            headerText.text = header;
            bodyText.text = body;
        }

        private void CreateButton(ButtonInfo buttonInfo)
        {
            Button newButton = Instantiate(buttonPrefab, buttonContainer);
            newButton.Init(buttonInfo);
        }
    }
}
