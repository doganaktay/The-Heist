using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Archi.Touch
{
    [System.Serializable]
    public struct DScreenDepth
    {
        public enum ConversionType
        {
            FixedDistance,
            DepthIntercept,
            PhysicsRaycast,
            PlaneIntercept,
            PathClosest,
            AutoDistance,
            HeightIntercept
        }

        public ConversionType Conversion;
        public Camera Cam;
        public Object Object;
        public LayerMask Mask;
        public float Distance;
        public static Vector3 LastWorldNormal = Vector3.forward;
        private static readonly RaycastHit[] hits = new RaycastHit[128];

        public DScreenDepth(ConversionType conversion, int newMask = Physics.DefaultRaycastLayers, float newDistance = 0f)
        {
            Conversion = conversion;
            Cam = null;
            Object = null;
            Mask = newMask;
            Distance = newDistance;
        }

        public Vector3 Convert(Vector2 screenPoint, GameObject go = null, Transform ignore = null)
        {
            var position = default(Vector3);
            TryConvert(ref position, screenPoint, go, ignore);
            return position;
        }

        public Vector3 ConvertDelta(Vector2 lastScreenPoint, Vector2 screenPoint, GameObject go = null, Transform ignore = null)
        {
            var lastWorldPoint = Convert(lastScreenPoint, go, ignore);
            var worldPoint = Convert(screenPoint, go, ignore);
            return worldPoint - lastWorldPoint;
        }

        public bool TryConvert(ref Vector3 position, Vector2 screenPoint, GameObject go = null, Transform ignore = null)
        {
            var cam = DTouch.GetCamera(Cam, go);

            if(cam != null)
            {
                switch (Conversion)
                {
                    case ConversionType.FixedDistance:
                        {
                            var screenPoint3 = new Vector3(screenPoint.x, screenPoint.y, Distance);
                            position = cam.ScreenToWorldPoint(screenPoint3);
                            LastWorldNormal = -cam.transform.forward;
                            return true;
                        }

                    case ConversionType.DepthIntercept:
                        {
                            var ray = cam.ScreenPointToRay(screenPoint);
                            var slope = -ray.direction.z;

                            if(slope != 0f)
                            {
                                var scale = (ray.origin.z - Distance) / slope;
                                position = ray.GetPoint(scale);
                                LastWorldNormal = Vector3.back;
                                return true;
                            }
                        }
                        break;

                    case ConversionType.PhysicsRaycast:
                        {
                            var ray = cam.ScreenPointToRay(screenPoint);
                            var hitCount = Physics.RaycastNonAlloc(ray, hits, float.PositiveInfinity, Mask);
                            var bestPoint = default(Vector3);
                            var bestDist = float.PositiveInfinity;

                            for(int i = hitCount - 1; i >= 0; i--)
                            {
                                var hit = hits[i];
                                var hitDistance = hit.distance;

                                if(hitDistance < bestDist && !IsChildOf(hit.transform, ignore))
                                {
                                    bestPoint = hit.point + hit.normal * Distance;
                                    bestDist = hitDistance;

                                    LastWorldNormal = hit.normal;
                                }
                            }

                            if(bestDist < float.PositiveInfinity)
                            {
                                position = bestPoint;
                                return true;
                            }
                        }
                        break;

                    case ConversionType.PlaneIntercept:
                        {
                            var plane = default(DPlane);
                            if(Exists(go, ref plane))
                            {
                                var ray = cam.ScreenPointToRay(screenPoint);
                                var hit = default(Vector3);

                                if(plane.TryRaycast(ray, ref hit, Distance))
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
                            var path = default(DPath);

                            if(Exists(go, ref path))
                            {
                                var ray = cam.ScreenPointToRay(screenPoint);

                                if(path.TryGetClosest(ray, ref position, -1, Distance * Time.deltaTime))
                                {
                                    LastWorldNormal = DPath.lastWorldNormal;
                                    return true;
                                }
                            }
                        }
                        break;

                    case ConversionType.AutoDistance:
                        {
                            if(go != null)
                            {
                                var depth = cam.WorldToScreenPoint(go.transform.position).z;
                                var screenPoint3 = new Vector3(screenPoint.x, screenPoint.y, depth + Distance);

                                position = cam.ScreenToWorldPoint(screenPoint3);
                                LastWorldNormal = -cam.transform.forward;

                                return true;
                            }
                        }
                        break;

                    case ConversionType.HeightIntercept:
                        {
                            var ray = cam.ScreenPointToRay(screenPoint);
                            var slope = -ray.direction.y;

                            if(slope != 0f)
                            {
                                var scale = (ray.origin.y - Distance) / slope;
                                position = ray.GetPoint(scale);
                                LastWorldNormal = Vector3.down;

                                return true;
                            }
                        }
                        break;
                }
            }
            else
            {
                Debug.LogError("Failed to find camera", go);
            }

            return false;
        }

        private bool Exists<T>(GameObject go, ref T instance)
            where T : Object
        {
            instance = Object as T;

            if(instance != null)
                return true;

            Object = instance = go.GetComponentInParent<T>();

            if(instance != null)
                return true;
            

            Object = instance = Object.FindObjectOfType<T>();

            if (instance != null)
                return true;

            return false;
        }

        private static bool IsChildOf(Transform current, Transform target)
        {
            if(target != null)
            {
                while (true)
                {
                    if (current == target)
                        return true;

                    current = current.parent;

                    if (current == null)
                        break;
                }
            }

            return false;
        }
    }
}

#if UNITY_EDITOR

namespace Archi.Touch
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(DScreenDepth))]
    public class DScreenDepth_Drawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var conversion = (DScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
            var height = base.GetPropertyHeight(property, label);
            var step = height + 2;

            switch (conversion)
            {
                case DScreenDepth.ConversionType.FixedDistance: height += step * 2; break;
                case DScreenDepth.ConversionType.DepthIntercept: height += step * 2; break;
                case DScreenDepth.ConversionType.PhysicsRaycast: height += step * 3; break;
                case DScreenDepth.ConversionType.PlaneIntercept: height += step * 3; break;
                case DScreenDepth.ConversionType.PathClosest: height += step * 3; break;
                case DScreenDepth.ConversionType.AutoDistance: height += step * 2; break;
                case DScreenDepth.ConversionType.HeightIntercept: height += step * 2; break;
            }

            return height;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var conversion = (DScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
            var height = base.GetPropertyHeight(property, label);

            rect.height = height;

            DrawProperty(ref rect, property, label, "Conversion", label.text, "The method used to convert between screen coordinates, and world coordinates." +
                "\n\nFixedDistance = A point will be projected out from the camera." +
                "\n\nDepthIntercept = A point will be intercepted out from the camera on a surface lying flat on the XY plane." +
                "\n\nPhysicsRaycast = A ray will be cast from the camera.\n\nPathClosest = A point will be intercepted out from the camera to the closest point on the specified path." +
                "\n\nAutoDistance = A point will be projected out from the camera based on the current Transform depth." +
                "\n\nHeightIntercept = A point will be intercepted out from the camera on a surface lying flat on the XZ plane.");

            EditorGUI.indentLevel++;
            {
                DrawProperty(ref rect, property, label, "Camera", null, "The camera the depth calculations will be done using.\n\nNone = MainCamera.");

                switch (conversion)
                {
                    case DScreenDepth.ConversionType.FixedDistance:
                        {
                            var color = GUI.color; if (property.FindPropertyRelative("Distance").floatValue == 0.0f) GUI.color = Color.red;
                            DrawProperty(ref rect, property, label, "Distance", "Distance", "The world space distance from the camera the point will be placed. This should be greater than 0.");
                            GUI.color = color;
                        }
                        break;

                    case DScreenDepth.ConversionType.DepthIntercept:
                        {
                            DrawProperty(ref rect, property, label, "Distance", "Z =", "The world space point along the Z axis the plane will be placed. For normal 2D scenes this should be 0.");
                        }
                        break;

                    case DScreenDepth.ConversionType.PhysicsRaycast:
                        {
                            var color = GUI.color; if (property.FindPropertyRelative("Layers").intValue == 0) GUI.color = Color.red;
                            DrawProperty(ref rect, property, label, "Layers", "The layers used in the raycast.");
                            GUI.color = color;
                            DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the raycast hit point.");
                        }
                        break;

                    case DScreenDepth.ConversionType.PlaneIntercept:
                        {
                            DrawObjectProperty<DPlane>(ref rect, property, "Plane", "The plane that will be intercepted.");
                            DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the intercept hit point.");
                        }
                        break;

                    case DScreenDepth.ConversionType.PathClosest:
                        {
                            DrawObjectProperty<DPath>(ref rect, property, "Path", "The path that will be intercepted.");
                            DrawProperty(ref rect, property, label, "Distance", "Max Delta", "The maximum amount of segments that can be moved between.");
                        }
                        break;

                    case DScreenDepth.ConversionType.AutoDistance:
                        {
                            DrawProperty(ref rect, property, label, "Distance", "Offset", "The depth offset from the calculated point.");
                        }
                        break;

                    case DScreenDepth.ConversionType.HeightIntercept:
                        {
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
            var propertyObject = property.FindPropertyRelative("Object");
            var oldValue = propertyObject.objectReferenceValue as T;

            var color = GUI.color; if (oldValue == null) GUI.color = Color.red;
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = propertyObject.hasMultipleDifferentValues;
            var newValue = EditorGUI.ObjectField(rect, new GUIContent(title, tooltip), oldValue, typeof(T), true);
            EditorGUI.showMixedValue = mixed;
            GUI.color = color;

            if(oldValue != newValue)
            {
                propertyObject.objectReferenceValue = newValue;
            }

            rect.y += rect.height;
        }

        private void DrawProperty(ref Rect rect, SerializedProperty property, GUIContent label,
            string childName, string overrideName = null, string overrideTooltip = null)
        {
            var childProperty = property.FindPropertyRelative(childName);
            label.text = !string.IsNullOrEmpty(overrideName) ? overrideName : childProperty.displayName;
            label.tooltip = !string.IsNullOrEmpty(overrideTooltip) ? overrideTooltip : childProperty.tooltip;
            EditorGUI.PropertyField(rect, childProperty, label);
            rect.y += rect.height + 2;
        }
    }
}

#endif
