using UnityEngine;

namespace Lean.Touch
{
    public abstract class CustomScreenDepth
    {
        public abstract void GetDepth(out Vector3 position, ref Vector3 lastWorldNormal);
    }

    /// <summary>This struct handles the conversion between screen coordinates, and world coordinates.
    /// This conversion is required for many touch interactions, and there are numerous ways it can be performed.</summary>
    [System.Serializable]
    public struct LeanScreenDepth
    {
        public enum ConversionType
        {
            FixedDistance,
            DepthIntercept,
            PhysicsRaycast,
            PlaneIntercept,
            PathClosest,
            AutoDistance,
            HeightIntercept,
            Custom
        }

        /// <summary>The method used to convert between screen coordinates, and world coordinates.
        /// FixedDistance = A point will be projected out from the camera.
        /// DepthIntercept = A point will be intercepted out from the camera on a surface lying flat on the XY plane.
        /// PhysicsRaycast = A ray will be cast from the camera.
        /// PathClosest = A point will be intercepted out from the camera to the closest point on the specified path.
        /// AutoDistance = A point will be projected out from the camera based on the current Transform depth.
        /// HeightIntercept = A point will be intercepted out from the camera on a surface lying flat on the XZ plane.</summary>
        public ConversionType Conversion;

        /// <summary>The camera the depth calculations will be done using.
        /// None = MainCamera.</summary>
        public Camera Camera;

        /// <summary>The plane/path/etc that will be intercepted.</summary>
        public Object Object;

        /// <summary>The layers used in the raycast.</summary>
        public LayerMask Layers;

        /// <summary>Toolips are modified at runtime based on Conversion setting.</summary>
        public float Distance;

        public CustomScreenDepth CustomDepth;

        /// <summary>When performing a ScreenDepth conversion, the converted point can have a normal associated with it. This stores that.</summary>
        public static Vector3 LastWorldNormal = Vector3.forward;

        private static readonly RaycastHit[] hits = new RaycastHit[128];

        public LeanScreenDepth(ConversionType newConversion, int newLayers = Physics.DefaultRaycastLayers, float newDistance = 0.0f)
        {
            Conversion = newConversion;
            Camera = null;
            Object = null;
            Layers = newLayers;
            Distance = newDistance;
            CustomDepth = null;
        }

        // This will do the actual conversion
        public Vector3 Convert(Vector2 screenPoint, GameObject gameObject = null, Transform ignore = null)
        {
            Vector3 position = default(Vector3);

            TryConvert(ref position, screenPoint, gameObject, ignore);

            return position;
        }

        // This will return the delta between two converted screenPoints
        public Vector3 ConvertDelta(Vector2 lastScreenPoint, Vector2 screenPoint, GameObject gameObject = null, Transform ignore = null)
        {
            Vector3 lastWorldPoint = Convert(lastScreenPoint, gameObject, ignore);
            Vector3 worldPoint = Convert(screenPoint, gameObject, ignore);

            return worldPoint - lastWorldPoint;
        }

        // This will do the actual conversion
        public bool TryConvert(ref Vector3 position, Vector2 screenPoint, GameObject gameObject = null, Transform ignore = null)
        {
            Camera camera = LeanTouch.GetCamera(Camera, gameObject);

            if (camera != null)
            {
                switch (Conversion)
                {
                    case ConversionType.FixedDistance:
                        {
                            Vector3 screenPoint3 = new Vector3(screenPoint.x, screenPoint.y, Distance);

                            position = camera.ScreenToWorldPoint(screenPoint3);

                            LastWorldNormal = -camera.transform.forward;

                            return true;
                        }

                    case ConversionType.DepthIntercept:
                        {
                            Ray ray = camera.ScreenPointToRay(screenPoint);
                            float slope = -ray.direction.z;

                            if (slope != 0.0f)
                            {
                                float scale = (ray.origin.z - Distance) / slope;

                                position = ray.GetPoint(scale);

                                LastWorldNormal = Vector3.back;

                                return true;
                            }
                        }
                        break;

                    case ConversionType.PhysicsRaycast:
                        {
                            Ray ray = camera.ScreenPointToRay(screenPoint);
                            int hitCount = Physics.RaycastNonAlloc(ray, hits, float.PositiveInfinity, Layers);
                            Vector3 bestPoint = default(Vector3);
                            float bestDist = float.PositiveInfinity;

                            for (int i = hitCount - 1; i >= 0; i--)
                            {
                                RaycastHit hit = hits[i];
                                float hitDistance = hit.distance;

                                if (hitDistance < bestDist && IsChildOf(hit.transform, ignore) == false)
                                {
                                    bestPoint = hit.point + hit.normal * Distance;
                                    bestDist = hitDistance;

                                    LastWorldNormal = hit.normal;
                                }
                            }

                            if (bestDist < float.PositiveInfinity)
                            {
                                position = bestPoint;

                                return true;
                            }
                        }
                        break;

                    case ConversionType.PlaneIntercept:
                        {
                            LeanPlane plane = default(LeanPlane);

                            if (Exists(gameObject, ref plane))
                            {
                                Ray ray = camera.ScreenPointToRay(screenPoint);
                                Vector3 hit = default(Vector3);

                                if (plane.TryRaycast(ray, ref hit, Distance))
                                {
                                    position = hit;

                                    LastWorldNormal = plane.transform.forward;

                                    return true;
                                }
                            }
                        }
                        break;

                    case ConversionType.PathClosest:
                        {
                            LeanPath path = default(LeanPath);

                            if (Exists(gameObject, ref path))
                            {
                                Ray ray = camera.ScreenPointToRay(screenPoint);

                                if (path.TryGetClosest(ray, ref position, -1, Distance * Time.deltaTime))
                                {
                                    LastWorldNormal = LeanPath.LastWorldNormal;

                                    return true;
                                }
                            }
                        }
                        break;

                    case ConversionType.AutoDistance:
                        {
                            if (gameObject != null)
                            {
                                float depth = camera.WorldToScreenPoint(gameObject.transform.position).z;
                                Vector3 screenPoint3 = new Vector3(screenPoint.x, screenPoint.y, depth + Distance);

                                position = camera.ScreenToWorldPoint(screenPoint3);

                                LastWorldNormal = -camera.transform.forward;

                                return true;
                            }
                        }
                        break;

                    case ConversionType.HeightIntercept:
                        {
                            Ray ray = camera.ScreenPointToRay(screenPoint);
                            float slope = -ray.direction.y;

                            if (slope != 0.0f)
                            {
                                float scale = (ray.origin.y - Distance) / slope;

                                position = ray.GetPoint(scale);

                                LastWorldNormal = Vector3.down;

                                return true;
                            }
                        }
                        break;

                    case ConversionType.Custom:
                        {
                            CustomDepth.GetDepth(out position, ref LastWorldNormal);
                        }
                        break;
                }
            }
            else
            {
                Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", gameObject);
            }

            return false;
        }

        // If the specified object doesn't exist, try and find it in the scene
        private bool Exists<T>(GameObject gameObject, ref T instance)
            where T : Object
        {
            instance = Object as T;

            // Already exists?
            if (instance != null)
            {
                return true;
            }

            // Exists in ancestor?
            Object = instance = gameObject.GetComponentInParent<T>();

            if (instance != null)
            {
                return true;
            }

            // Exists in scene?
            Object = instance = Object.FindObjectOfType<T>();

            if (instance != null)
            {
                return true;
            }

            // Doesn't exist
            return false;
        }

        // This will return true if current or one of its parents matches the specified gameObject's Transform (current must be non-null)
        private static bool IsChildOf(Transform current, Transform target)
        {
            if (target != null)
            {
                while (true)
                {
                    if (current == target)
                    {
                        return true;
                    }

                    current = current.parent;

                    if (current == null)
                    {
                        break;
                    }
                }
            }

            return false;
        }
    }
}

#if UNITY_EDITOR
namespace Lean.Touch
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(LeanScreenDepth))]
    public class LeanScreenDepth_Drawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            LeanScreenDepth.ConversionType conversion = (LeanScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
            float height = base.GetPropertyHeight(property, label);
            float step = height + 2;

            switch (conversion)
            {
                case LeanScreenDepth.ConversionType.FixedDistance: height += step * 2; break;
                case LeanScreenDepth.ConversionType.DepthIntercept: height += step * 2; break;
                case LeanScreenDepth.ConversionType.PhysicsRaycast: height += step * 3; break;
                case LeanScreenDepth.ConversionType.PlaneIntercept: height += step * 3; break;
                case LeanScreenDepth.ConversionType.PathClosest: height += step * 3; break;
                case LeanScreenDepth.ConversionType.AutoDistance: height += step * 2; break;
                case LeanScreenDepth.ConversionType.HeightIntercept: height += step * 2; break;
                case LeanScreenDepth.ConversionType.Custom: height += step * 2; break;
            }

            return height;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            LeanScreenDepth.ConversionType conversion = (LeanScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
            float height = base.GetPropertyHeight(property, label);

            rect.height = height;

            DrawProperty(ref rect, property, label, "Conversion", label.text, "The method used to convert between screen coordinates, and world coordinates.\n\nFixedDistance = A point will be projected out from the camera.\n\nDepthIntercept = A point will be intercepted out from the camera on a surface lying flat on the XY plane.\n\nPhysicsRaycast = A ray will be cast from the camera.\n\nPathClosest = A point will be intercepted out from the camera to the closest point on the specified path.\n\nAutoDistance = A point will be projected out from the camera based on the current Transform depth.\n\nHeightIntercept = A point will be intercepted out from the camera on a surface lying flat on the XZ plane.");

            EditorGUI.indentLevel++;
            {
                DrawProperty(ref rect, property, label, "Camera", null, "The camera the depth calculations will be done using.\n\nNone = MainCamera.");

                switch (conversion)
                {
                    case LeanScreenDepth.ConversionType.FixedDistance:
                        {
                            Color color = GUI.color; if (property.FindPropertyRelative("Distance").floatValue == 0.0f) GUI.color = Color.red;
                            DrawProperty(ref rect, property, label, "Distance", "Distance", "The world space distance from the camera the point will be placed. This should be greater than 0.");
                            GUI.color = color;
                        }
                        break;

                    case LeanScreenDepth.ConversionType.DepthIntercept:
                        {
                            DrawProperty(ref rect, property, label, "Distance", "Z =", "The world space point along the Z axis the plane will be placed. For normal 2D scenes this should be 0.");
                        }
                        break;

                    case LeanScreenDepth.ConversionType.PhysicsRaycast:
                        {
                            Color color = GUI.color; if (property.FindPropertyRelative("Layers").intValue == 0) GUI.color = Color.red;
                            DrawProperty(ref rect, property, label, "Layers", "The layers used in the raycast.");
                            GUI.color = color;
                            DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the raycast hit point.");
                        }
                        break;

                    case LeanScreenDepth.ConversionType.PlaneIntercept:
                        {
                            DrawObjectProperty<LeanPlane>(ref rect, property, "Plane", "The plane that will be intercepted.");
                            DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the intercept hit point.");
                        }
                        break;

                    case LeanScreenDepth.ConversionType.PathClosest:
                        {
                            DrawObjectProperty<LeanPath>(ref rect, property, "Path", "The path that will be intercepted.");
                            DrawProperty(ref rect, property, label, "Distance", "Max Delta", "The maximum amount of segments that can be moved between.");
                        }
                        break;

                    case LeanScreenDepth.ConversionType.AutoDistance:
                        {
                            DrawProperty(ref rect, property, label, "Distance", "Offset", "The depth offset from the calculated point.");
                        }
                        break;

                    case LeanScreenDepth.ConversionType.HeightIntercept:
                        {
                            DrawProperty(ref rect, property, label, "Distance", "Y =", "The world space point along the Y axis the plane will be placed. For normal top down scenes this should be 0.");
                        }
                        break;
                    case LeanScreenDepth.ConversionType.Custom:
                        {
                            Color color = GUI.color;

                            DrawProperty(ref rect, property, label, "Distance", "Y =", "The world space point along the Y axis the plane will be placed. For normal top down scenes this should be 0.");
                        }
                        break;
                }
            }
            EditorGUI.indentLevel--;
        }

        private void DrawObjectProperty<T>(ref Rect rect, SerializedProperty property, string title, string tooltip)
            where T : Object
        {
            SerializedProperty propertyObject = property.FindPropertyRelative("Object");
            T oldValue = propertyObject.objectReferenceValue as T;

            Color color = GUI.color; if (oldValue == null) GUI.color = Color.red;
            bool mixed = EditorGUI.showMixedValue; EditorGUI.showMixedValue = propertyObject.hasMultipleDifferentValues;
            Object newValue = EditorGUI.ObjectField(rect, new GUIContent(title, tooltip), oldValue, typeof(T), true);
            EditorGUI.showMixedValue = mixed;
            GUI.color = color;

            if (oldValue != newValue)
            {
                propertyObject.objectReferenceValue = newValue;
            }

            rect.y += rect.height;
        }

        private void DrawProperty(ref Rect rect, SerializedProperty property, GUIContent label, string childName, string overrideName = null, string overrideTooltip = null)
        {
            SerializedProperty childProperty = property.FindPropertyRelative(childName);

            label.text = string.IsNullOrEmpty(overrideName) == false ? overrideName : childProperty.displayName;

            label.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : childProperty.tooltip;

            EditorGUI.PropertyField(rect, childProperty, label);

            rect.y += rect.height + 2;
        }
    }
}
#endif