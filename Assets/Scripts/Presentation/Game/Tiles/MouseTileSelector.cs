using System;
using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Tiles
{
    internal class MouseTileSelector : MonoBehaviour, ITileSelector, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ScreenToWorldPointConverter screenToWorldPointConverter;
        [SerializeField] private Positioner positioner;

        public event Action<GridVector> Selected;
        public event Action<bool> Dragging;

        private GridVector _lastDraggedOverPosition = new GridVector(-1, -1);

        public void OnBeginDrag(PointerEventData eventData)
        {
            Dragging.Invoke(true);
            OnDrag_Internal(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDrag_Internal(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Dragging.Invoke(false);
            OnDrag_Internal(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            GridVector position = GetGridPosition(eventData);
            Selected?.Invoke(position);
        }

        private void OnDrag_Internal(PointerEventData eventData)
        {
            GridVector position = GetGridPosition(eventData);

            if (position == _lastDraggedOverPosition)
            {
                return;
            }

            Selected.Invoke(position);

            _lastDraggedOverPosition = position;
        }

        private GridVector GetGridPosition(PointerEventData eventData)
        {
            Vector3 worldPos = screenToWorldPointConverter.GetWorldPoint(eventData.position);
            GridVector position = positioner.GetGridPosition(worldPos);
            return position;
        }
    }
}
