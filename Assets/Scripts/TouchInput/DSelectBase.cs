using UnityEngine;

namespace Archi.Touch
{
    public abstract class DSelectBase : MonoBehaviour
    {
        public enum SelectType
        {
            None = -1,
            Raycast3D,
            Overlap2D,
            CanvasUI,
            ScreenDistance,
            Intersect2D
        }

        public enum SearchType
        {
            GetComponent,
            GetComponentInParent,
            GetComponentInChildren
        }

        public SelectType SelectUsing;
        public SelectType SelectUsingAlt = SelectType.None;
        public SelectType SelectUsingAltAlt = SelectType.None;

        public SearchType Search = SearchType.GetComponentInParent;

        public Camera cam;
        public LayerMask mask = Physics.DefaultRaycastLayers;
        public string requiredTag;
        public float maxScreenDistance = 50f;

        private static RaycastHit[] raycastHits = new RaycastHit[1024];
        private static RaycastHit2D[] raycastHits2D = new RaycastHit2D[1024];

        public void SelectStartScreenPos(DFinger finger)
        {
            SelectScreenPos(finger, finger.startScreenPos);
        }

        public void SelectScreenPos(DFinger finger)
        {
            SelectScreenPos(finger, finger.screenPos);
        }

        public void SelectScreenPos(DFinger finger, Vector2 screenPos)
        {
            var component = default(Component);
            var worldPos = default(Vector3);

            TryGetComponent(SelectUsing, screenPos, ref component, ref worldPos);

            if(component == null)
            {
                TryGetComponent(SelectUsingAlt, screenPos, ref component, ref worldPos);

                if(component == null)
                    TryGetComponent(SelectUsingAltAlt, screenPos, ref component, ref worldPos);
            }

            TrySelect(finger, component, worldPos);
        }

        protected abstract void TrySelect(DFinger finger, Component component, Vector3 worldPos);

        protected void TryGetComponent(SelectType selectUsing, Vector2 screenPos, ref Component component, ref Vector3 worldPos)
        {
            switch (selectUsing)
            {
                case SelectType.Raycast3D:
                    {
                        var cam = DTouch.GetCamera(this.cam, gameObject);

                        if (cam != null)
                        {
                            var ray = cam.ScreenPointToRay(screenPos);
                            var count = Physics.RaycastNonAlloc(ray, raycastHits, float.PositiveInfinity, mask);

                            if(count > 0)
                            {
                                var closestHit = raycastHits[GetClosestRaycastHitIndex(count)];
                                component = closestHit.transform;
                                worldPos = closestHit.point;
                            }
                        }
                        else
                            Debug.LogError("Failed to find camera");
                    }
                    break;

                case SelectType.Overlap2D:
                    {
                        var cam = DTouch.GetCamera(this.cam, gameObject);

                        if(cam != null)
                        {
                            var ray = cam.ScreenPointToRay(screenPos);
                            var slope = -ray.direction.z;

                            if(slope != 0f)
                            {
                                var point = ray.GetPoint(ray.origin.z / slope);
                                component = Physics2D.OverlapPoint(point, mask);

                                if (component != null)
                                    worldPos = component.transform.position;
                            }
                        }
                        else
                            Debug.LogError("Failed to find camera");
                    }
                    break;

                case SelectType.CanvasUI:
                    {
                        var results = DTouch.RaycastGUI(screenPos, mask);

                        if(results != null && results.Count > 0)
                        {
                            var firstTransform = results[0].gameObject.transform;
                            component = firstTransform;
                            worldPos = firstTransform.position;
                        }
                    }
                    break;

                case SelectType.ScreenDistance:
                    {
                        var bestDistance = maxScreenDistance * DTouch.ScalingFactor;
                        bestDistance *= bestDistance;

                        var cam = DTouch.GetCamera(this.cam, gameObject);

                        if(cam != null)
                        {
                            foreach(var selectable in DSelectable.instances)
                            {
                                var distance = Vector2.SqrMagnitude(GetScreenPoint(cam, selectable.transform) - screenPos);

                                if(distance <= bestDistance)
                                {
                                    bestDistance = distance;
                                    component = selectable;
                                    worldPos = selectable.transform.position;
                                }
                            }
                        }
                    }
                    break;

                case SelectType.Intersect2D:
                    {
                        var cam = DTouch.GetCamera(this.cam, gameObject);

                        if(cam != null)
                        {
                            var ray = cam.ScreenPointToRay(screenPos);
                            var count = Physics2D.GetRayIntersectionNonAlloc(ray, raycastHits2D, float.PositiveInfinity, mask);

                            if(count > 0)
                            {
                                var firstHit = raycastHits2D[0];
                                component = firstHit.transform;
                                worldPos = firstHit.point;
                            }
                        }
                        else
                            Debug.LogError("Failed to find camera");
                    }
                    break;
            }
        }

        private static int GetClosestRaycastHitIndex(int count)
        {
            var closestIndex = -1;
            var closestDistance = float.PositiveInfinity;

            for(int i = 0; i < count; i++)
            {
                var distance = raycastHits[i].distance;

                if(distance < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = distance;
                }
            }

            return closestIndex;
        }

        private static Vector2 GetScreenPoint(Camera cam, Transform transform)
        {
            if(transform is RectTransform)
            {
                var canvas = transform.GetComponentInParent<Canvas>();

                if(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return RectTransformUtility.WorldToScreenPoint(null, transform.position);
                }
            }

            return cam.WorldToScreenPoint(transform.position);
        }
    }
}

#if UNITY_EDITOR

namespace Archi.Touch.Inspector
{
    using UnityEditor;

    public abstract class DSelectBase_Inspector<T> : Archi.Common.DInspector<T>
        where T : DSelectBase
    {
        protected override void DrawInspector()
        {
            Draw("SelectUsing", "Which kinds of objects should be selectable from this component?");
            Draw("SelectUsingAlt", "If SelectUsing fails, you can set an alternative method here.");
            Draw("SelectUsingAltAlt", "If SelectUsingAlt fails, you can set an alternative method here.");

            EditorGUILayout.Separator();

            Draw("Search", "How should the candidate GameObjects be searched for the LeanSelectable component?");
            Draw("Camera", "The camera used to calculate the ray.\n\nNone = MainCamera.");
            Draw("LayerMask", "The layers you want the raycast/overlap to hit.");
            Draw("RequiredTag", "The tag required for an object to be selected.");
            Draw("MaxScreenDistance", "When using the ScreenDistance selection mode, this allows you to set how many scaled pixels from the mouse/finger you can select.");
        }
    }
}

#endif