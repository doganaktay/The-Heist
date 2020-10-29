using System.Collections.Generic;
using UnityEngine;

namespace Archi.Touch
{
    public class DPath : MonoBehaviour
    {
        [Tooltip("Points along the path")]
        public List<Vector3> points;
        [Tooltip("Does the path loop?")]
        public bool loop;
        [Tooltip("The coordinate system for the points")]
        public Space space = Space.Self;
        [Tooltip("The amount of lines between each path point when read from DScreenDepth")]
        public int smoothing = 1;
        [Tooltip("Assign a line renderer to visualize path")]
        public LineRenderer visual;

        public static Vector3 lastWorldNormal = Vector3.forward;

        public int PointCount
        {
            get
            {
                if(points != null)
                {
                    var count = points.Count;

                    if(count >= 2)
                    {
                        if (loop)
                            return count + 1;
                        else
                            return count;
                    }
                }

                return 0;
            }
        }

        public int GetPointCount(int smoothing = -1)
        {
            if(points != null)
            {
                if (smoothing < 0)
                    smoothing = this.smoothing;

                var count = points.Count;

                if(count >= 2 && smoothing >= 1)
                {
                    if (loop)
                        return count * smoothing + 1;
                    else
                        return (count - 1) * smoothing + 1;
                }
            }

            return 0;
        }

        public Vector3 GetSmoothedPoint(float index)
        {
            if (points == null)
                throw new System.IndexOutOfRangeException();

            var count = points.Count;

            if (count < 2)
                throw new System.Exception();

            // get int and fractional part of index
            var i = (int)index;
            var t = Mathf.Abs(index - i);

            // get 4 control points
            var a = GetPointRaw(i - 1, count);
            var b = GetPointRaw(i, count);
            var c = GetPointRaw(i + 1, count);
            var d = GetPointRaw(i + 2, count);

            // interpolate and return
            var p = default(Vector3);

            p.x = CubicInterpolate(a.x, b.x, c.x, d.x, t);
            p.y = CubicInterpolate(a.y, b.y, c.y, d.y, t);
            p.z = CubicInterpolate(a.z, b.z, c.z, d.z, t);

            return p;
        }

        public Vector3 GetPoint(int index, int smoothing = -1)
        {
            if (points == null)
                throw new System.IndexOutOfRangeException();

            if (smoothing < 0)
                smoothing = this.smoothing;

            if (smoothing < 1)
                throw new System.ArgumentOutOfRangeException();

            var count = points.Count;

            if (count < 2)
                throw new System.Exception();

            if (smoothing > 0)
                return GetSmoothedPoint(index / (float)smoothing);

            return GetPointRaw(index, count);
        }

        private Vector3 GetPointRaw(int index, int count)
        {
            if (loop)
                index = Mod(index, count);
            else
                index = Mathf.Clamp(index, 0, count - 1);

            var point = points[index];
            if (space == Space.Self)
                point = transform.TransformPoint(point);

            return point;
        }

        public void SetLine(Vector3 a, Vector3 b)
        {
            if (points == null)
                points = new List<Vector3>();
            else
                points.Clear();

            points.Add(a);
            points.Add(b);
        }

        public bool TryGetClosest(Vector3 position, ref Vector3 closestPoint, ref int closestIndexA, ref int closestIndexB, int smoothing = -1)
        {
            var count = GetPointCount(smoothing);

            if(count >= 2)
            {
                var indexA = 0;
                var pointA = GetPoint(indexA, smoothing);
                var closestDistance = float.PositiveInfinity;

                for(int i = 1; i < count; i++)
                {
                    var indexB = i;
                    var pointB = GetPoint(indexB, smoothing);
                    var point = GetClosestPoint(position, pointA, pointB - pointA);
                    var distance = Vector3.Distance(position, point);

                    if(distance < closestDistance)
                    {
                        closestIndexA = indexA;
                        closestIndexB = i;
                        closestPoint = point;
                        closestDistance = distance;
                        lastWorldNormal = Vector3.Normalize(point - pointB);
                    }

                    pointA = pointB;
                    indexA = indexB;
                }

                return true;
            }

            return false;
        }

        public bool TryGetClosest(Vector3 position, ref Vector3 closestPoint, int smoothing = -1)
        {
            var closestIndexA = default(int);
            var closestIndexB = default(int);

            return TryGetClosest(position, ref closestPoint, ref closestIndexA, ref closestIndexB, smoothing);
        }

        public bool TryGetClosest(Ray ray, ref Vector3 closestPoint, ref int closestIndexA, ref int closestIndexB, int smoothing = -1)
        {
            var count = GetPointCount(smoothing);

            if(count >= 2)
            {
                var indexA = 0;
                var pointA = GetPoint(0, smoothing);
                var closestDistance = float.PositiveInfinity;

                for(int i = 1; i < count; i++)
                {
                    var pointB = GetPoint(i, smoothing);
                    var point = GetClosestPoint(ray, pointA, pointB - pointA);
                    var distance = GetClosestDistance(ray, point);

                    if(distance < closestDistance)
                    {
                        closestIndexA = indexA;
                        closestIndexB = i;
                        closestPoint = point;
                        closestDistance = distance;
                        lastWorldNormal = Vector3.Normalize(point - pointB);
                    }

                    pointA = pointB;
                    indexA = i;
                }

                return true;
            }

            return false;
        }

        public bool TryGetClosest(Ray ray, ref Vector3 currentPoint, int smoothing = -1)
        {
            var closestIndexA = default(int);
            var closestIndexB = default(int);

            return TryGetClosest(ray, ref currentPoint, ref closestIndexA, ref closestIndexB, smoothing);
        }

        public bool TryGetClosest(Ray ray, ref Vector3 currentPoint, int smoothing = -1, float maxDelta = -1f)
        {
            if(maxDelta > 0f)
            {
                var closestPoint = currentPoint;

                if(TryGetClosest(ray, ref closestPoint, smoothing))
                {
                    var targetPoint = Vector3.MoveTowards(currentPoint, closestPoint, maxDelta);
                    return TryGetClosest(targetPoint, ref currentPoint, smoothing);
                }

                return false;
            }

            return TryGetClosest(ray, ref currentPoint, smoothing);
        }

        private Vector3 GetClosestPoint(Vector3 position, Vector3 origin, Vector3 direction)
        {
            var denom = Vector3.Dot(direction, direction);

            if (denom == 0f)
                return origin;

            var dist01 = Vector3.Dot(position - origin, direction) / denom;

            return origin + direction * Mathf.Clamp01(dist01);
        }

        private Vector3 GetClosestPoint(Ray ray, Vector3 origin, Vector3 direction)
        {
            var crossA = Vector3.Cross(ray.direction, direction);
            var denom = Vector3.Dot(crossA, crossA);

            if (denom == 0f)
                return origin;

            var crossB = Vector3.Cross(ray.direction, ray.origin - origin);
            var dist01 = Vector3.Dot(crossA, crossB) / denom;

            return origin + direction * Mathf.Clamp01(dist01);
        }

        private float GetClosestDistance(Ray ray, Vector3 point)
        {
            var denom = Vector3.Dot(ray.direction, ray.direction);

            if (denom == 0f)
                return Vector3.Distance(ray.origin, point);

            var dist01 = Vector3.Dot(point - ray.origin, ray.direction) / denom;

            return Vector3.Distance(point, ray.GetPoint(dist01));
        }

        private int Mod(int a, int b)
        {
            a %= b;
            return a < 0 ? a + b : a;
        }

        private float CubicInterpolate(float a, float b, float c, float d, float t)
        {
            var tt = t * t;
            var ttt = tt * t;

            var e = a - b;
            var f = d - c;
            var g = f - e;
            var h = e - g;
            var i = c - a;

            return g * ttt + h * tt + i * t + b;
        }

        public void UpdateVisual()
        {
            if(visual != null)
            {
                var count = GetPointCount();

                visual.positionCount = count;

                for(int i = 0; i < count; i++)
                {
                    visual.SetPosition(i, GetPoint(i));
                }
            }
        }

        protected virtual void Update()
        {
            UpdateVisual();
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            var count = GetPointCount();

            if(count >= 2)
            {
                var pointA = GetPoint(0);

                for(int i = 1; i < count; i++)
                {
                    var pointB = GetPoint(i);

                    Gizmos.DrawLine(pointA, pointB);

                    pointA = pointB;
                }
            }
        }
#endif
    }
}
