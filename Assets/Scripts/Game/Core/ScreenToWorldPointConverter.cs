using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace Game.Core
{
    [Serializable]
    [ExecuteInEditMode]
    public class ScreenToWorldPointConverter : MonoBehaviour/*, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler*/
    {
        [SerializeField] private new UnityEngine.Camera camera;

        [SerializeField] private bool useTransformXZForPlane;

        [SerializeField, HideIf(nameof(useTransformXZForPlane))] private Vector3 planeNormal;
        [SerializeField, HideIf(nameof(useTransformXZForPlane))] private float planeDistance;

        [SerializeField] private float gizmoPlaneSize = 10;

        [ShowInInspector] private string ScreenPos => $"x: {UnityInput.mousePosition.x}, y: {UnityInput.mousePosition.y}";
        [ShowInInspector]
        private string WorldPos
        {
            get
            {
                Vector3 worldPos = GetWorldPoint(UnityInput.mousePosition);
                return $"x: {worldPos.x}, y: {worldPos.y}, z: {worldPos.z}";
            }
        }

        private Plane _plane;

        #region Unity

        private void Awake()
        {
            CreatePlane();
        }

        private void OnValidate()
        {
            CreatePlane();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 planeUp, planeCenter, planeSide, planeForward;
            GetPlaneInfo(out planeUp, out planeCenter, out planeSide, out planeForward);

            Vector3 corner0 = planeCenter + (planeSide + planeForward) * gizmoPlaneSize;
            Vector3 forwardSide = (planeForward + planeForward) * gizmoPlaneSize;
            Vector3 sideSide = (planeSide + planeSide) * gizmoPlaneSize;
            Vector3 corner1 = corner0 - forwardSide;
            Vector3 corner2 = corner1 - sideSide;
            Vector3 corner3 = corner2 + forwardSide;
            Vector3 corner4 = corner3 + sideSide;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(planeCenter, planeCenter + planeUp * gizmoPlaneSize);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(corner0, corner1);
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner0, corner2);
            Gizmos.DrawLine(corner1, corner3);

            Gizmos.DrawSphere(GetWorldPoint(UnityInput.mousePosition), 0.1f);
        }

        #endregion

        #region API

        public Vector3 GetWorldPoint(Vector2 screenPoint)
        {
            Ray ray = camera.ScreenPointToRay(screenPoint);
            if (_plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return default;
        }

        #endregion

        #region Event System Handlers

        //public void OnPointerClick(PointerEventData eventData)
        //{

        //}

        //public void OnBeginDrag(PointerEventData eventData)
        //{
        //    throw new NotImplementedException();
        //}

        //public void OnDrag(PointerEventData eventData)
        //{
        //    throw new NotImplementedException();
        //}

        //public void OnEndDrag(PointerEventData eventData)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion

        #region Methods

        private void GetPlaneInfo(out Vector3 planeUp, out Vector3 planeCenter, out Vector3 planeSide, out Vector3 planeForward)
        {
            if (useTransformXZForPlane)
            {
                GetTransformPlaneInfo(out planeUp, out planeCenter, out planeSide, out planeForward);
            }
            else
            {
                GetManualPlaneInfo(out planeUp, out planeCenter, out planeSide, out planeForward);
            }
        }

        private void GetTransformPlaneInfo(out Vector3 planeUp, out Vector3 planeCenter, out Vector3 planeSide, out Vector3 planeForward)
        {
            planeUp = transform.up;
            planeCenter = transform.position;
            planeSide = transform.right;
            planeForward = transform.forward;
        }

        private void GetManualPlaneInfo(out Vector3 planeUp, out Vector3 planeCenter, out Vector3 planeSide, out Vector3 planeForward)
        {
            planeUp = planeNormal.normalized;
            planeCenter = planeUp * planeDistance;
            planeSide = Vector3.Cross(planeUp, Vector3.forward).normalized;
            planeForward = Vector3.Cross(planeUp, planeSide).normalized;
        }

        private void CreatePlane()
        {
            if (useTransformXZForPlane)
            {
                _plane = new Plane(transform.up, transform.position);
            }
            else
            {
                _plane = new Plane(planeNormal, this.planeDistance);
            }
        }

        #endregion
    }
}