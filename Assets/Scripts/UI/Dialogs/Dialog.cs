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

        private Action _closeCallback;

        public int Id { get; private set; }

        public void Init(int id, DialogInfo info, Action closeCallback)
        {
            Id = id;

            SetText(info.header, info.Body);

            CreateButton(info.Button1, closeCallback);
            if (!string.IsNullOrWhiteSpace(info.Button2.Message))
            {
                CreateButton(info.Button2, closeCallback);
            }

            _closeCallback = closeCallback;
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

        private void CreateButton(ButtonInfo buttonInfo, Action closeCallback)
        {
            Button newButton = Instantiate(buttonPrefab, buttonContainer);
            buttonInfo.AppendAction(closeCallback);
            
            newButton.Init(buttonInfo);
        }
    }
}
