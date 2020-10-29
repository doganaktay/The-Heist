using UnityEngine;

namespace Archi.Touch
{
    public class DPlane : MonoBehaviour
    {
        [Tooltip("Clamp the plane on the x axis?")]
        public bool clampX;
        public float minX, maxX;
        [Space]
        [Tooltip("Clamp the plane on the y axis?")]
        public bool clampY;
        public float minY, maxY;
        [Space]
        [Tooltip("Distance between position snap on x axis")]
        public float snapX;
        [Tooltip("Distance between position snap on y axis")]
        public float snapY;

        public Vector3 GetClosest(Vector3 pos, float offset = 0f)
        {
            var point = transform.InverseTransformPoint(pos);

            if (clampX)
                point.x = Mathf.Clamp(point.x, minX, maxX);
            if (clampY)
                point.y = Mathf.Clamp(point.y, minY, maxY);

            if (snapX != 0f)
                point.x = Mathf.Round(point.x / snapX) * snapX;
            if (snapY != 0f)
                point.y = Mathf.Round(point.y / snapY) * snapY;

            point.z = 0f;

            return transform.TransformPoint(point) + transform.forward * offset;
        }

        public Vector3 GetClosest(Ray ray)
        {
            var point = transform.position;
            var normal = transform.forward;
            var distance = default(float);

            if (RayToPlane(point, normal, ray, ref distance))
                return GetClosest(ray.GetPoint(distance));

            return point;
        }

        public bool TryRaycast(Ray ray, ref Vector3 hit, float offset = 0f, bool getClosest = true)
        {
            var point = transform.position;
            var normal = transform.forward;
            var distance = default(float);

            if(RayToPlane(point, normal, ray, ref distance))
            {
                hit = ray.GetPoint(distance);

                if (getClosest)
                    hit = GetClosest(hit, offset);

                return true;
            }

            return false;
        }

        private static bool RayToPlane(Vector3 point, Vector3 normal, Ray ray, ref float distance)
        {
            var b = Vector3.Dot(ray.direction, normal);

            if (Mathf.Approximately(b, 0f))
                return false;

            var d = -Vector3.Dot(normal, point);
            var a = -Vector3.Dot(ray.origin, normal) - d;

            distance = a / b;

            return distance > 0f;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            var x1 = minX;
            var x2 = maxX;
            var y1 = minY;
            var y2 = maxY;

            if (!clampX)
            {
                x1 = -1000f;
                x2 = 1000f;
            }
            if (!clampY)
            {
                y1 = -1000f;
                y2 = 1000f;
            }

            if (!clampX && !clampY)
            {
                Gizmos.DrawLine(new Vector3(x1, 0f), new Vector3(x2, 0f));
                Gizmos.DrawLine(new Vector3(0f, y1), new Vector3(0f, y2));
            }
            else
            {
                Gizmos.DrawLine(new Vector3(x1, y1), new Vector3(x2, y1));
                Gizmos.DrawLine(new Vector3(x1, y2), new Vector3(x2, y2));
                Gizmos.DrawLine(new Vector3(x1, y1), new Vector3(x1, y2));
                Gizmos.DrawLine(new Vector3(x2, y1), new Vector3(x2, y2));
            }
        }
#endif
    }
}
