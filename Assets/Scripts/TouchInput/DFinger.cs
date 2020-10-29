// the Archi.Touch namespace is a rewrite of Lean Touch (for practice)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.Touch
{
    public class DFinger
    {
        public int index;
        public float age;
        public bool set, lastSet;
        public bool tap;
        public int tapCount;
        public bool swipe;
        public bool old;
        public bool expired;
        public Vector2 startScreenPos, lastScreenPos, screenPos;
        public bool startedOverGUI;
        public List<DSnapshot> snapshots = new List<DSnapshot>();

        #region getters

        public bool IsActive { get { return DTouch.fingers.Contains(this); } }
        public float SnapshotDuration { get { if (snapshots.Count > 0) { return age - snapshots[0].age; } return 0f; } }
        public bool IsOverGUI { get { return DTouch.PointOverGUI(screenPos); } }
        public bool Down { get { return set == true && lastSet == false; } }
        public bool Up { get { return set == false && lastSet == true; } }

        public Vector2 LastSnapshotScreenDelta
        {
            get
            {
                var count = snapshots.Count;

                if(count > 0)
                {
                    var snapshot = snapshots[count - 1];

                    if(snapshot != null)
                    {
                        return screenPos - snapshot.screenPos;
                    }
                }

                return Vector2.zero;
            }
        }

        public Vector2 LastSnapshotScaledDelta
        {
            get
            {
                return LastSnapshotScreenDelta * DTouch.ScalingFactor;
            }
        }

        public Vector2 ScreenDelta
        {
            get
            {
                return screenPos - lastScreenPos;
            }
        }

        public Vector2 ScaledDelta
        {
            get
            {
                return ScreenDelta * DTouch.ScalingFactor;
            }
        }

        public Vector2 SwipeScreenDelta
        {
            get
            {
                return screenPos - startScreenPos;
            }
        }

        public Vector2 SwipeScaledDelta
        {
            get
            {
                return SwipeScreenDelta * DTouch.ScalingFactor;
            }
        }

        public float SmoothScreenPosDelta
        {
            get
            {
                if(snapshots.Count > 0 && set == true)
                {
                    var c = snapshots[Mathf.Max(0, snapshots.Count - 3)].screenPos;
                    var b = snapshots[Mathf.Max(0, snapshots.Count - 2)].screenPos;

                    return Vector2.Distance(c, b);
                }

                return Vector2.Distance(lastScreenPos, screenPos);
            }
        }

        #endregion getters

        #region methods

        public Ray GetRay(Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if(cam != null)
            {
                return cam.ScreenPointToRay(screenPos);
            }
            else
            {
                Debug.LogError("Failed to find camera");
            }

            return default(Ray);
        }

        public Ray GetStartRay(Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if(cam != null)
            {
                return cam.ScreenPointToRay(startScreenPos);
            }
            else
            {
                Debug.LogError("Failed to find camera");
            }

            return default(Ray);
        }

        public Vector2 GetSnapshotScreenDelta(float deltaTime)
        {
            return screenPos - GetSnapshotScreenPos(age - deltaTime);
        }

        public Vector2 GetSnapshotScaledDelta(float deltaTime)
        {
            return GetSnapshotScreenDelta(deltaTime) * DTouch.ScalingFactor;
        }

        public Vector2 GetSnapshotScreenPos(float targetAge)
        {
            var pos = screenPos;

            DSnapshot.TryGetScreenPos(snapshots, targetAge, ref pos);

            return pos;
        }

        public Vector3 GetSnapshotWorldPos(float targetAge, float distance, Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if(cam != null)
            {
                var pos = GetSnapshotScreenPos(targetAge);
                var point = new Vector3(screenPos.x, screenPos.y, distance);

                return cam.ScreenToWorldPoint(point);
            }
            else
            {
                Debug.LogError("Failed to find camera");
            }

            return default(Vector3);
        }

        public float GetRadians(Vector2 referencePoint)
        {
            return Mathf.Atan2(screenPos.x - referencePoint.x, screenPos.y - referencePoint.y);
        }

        public float GetDegrees(Vector2 referencePoint)
        {
            return GetRadians(referencePoint) * Mathf.Rad2Deg;
        }

        public float GetLastRadians(Vector2 referencePoint)
        {
            return Mathf.Atan2(lastScreenPos.x - referencePoint.x, lastScreenPos.y - referencePoint.y);
        }

        public float GetLastDegrees(Vector2 referencePoint)
        {
            return GetLastRadians(referencePoint) * Mathf.Rad2Deg;
        }

        public float GetDeltaRadians(Vector2 referencePoint, Vector2 lastReferencePoint)
        {
            var a = GetLastRadians(lastReferencePoint);
            var b = GetRadians(referencePoint);
            var d = Mathf.Repeat(a - b, Mathf.PI * 2f);

            if(d > Mathf.PI)
            {
                d -= Mathf.PI * 2f;
            }

            return d;
        }

        public float GetDeltaDegrees(Vector2 referencePoint)
        {
            return GetDeltaRadians(referencePoint, referencePoint) * Mathf.Rad2Deg;
        }

        public float GetDeltaDegrees(Vector2 referencePoint, Vector2 lastReferencePoint)
        {
            return GetDeltaRadians(referencePoint, lastReferencePoint) * Mathf.Rad2Deg;
        }

        public float GetScreenDistance(Vector2 point)
        {
            return Vector2.Distance(screenPos, point);
        }

        public float GetScaledDistance(Vector2 point)
        {
            return GetScreenDistance(point) * DTouch.ScalingFactor;
        }

        public float GetLastScreenDistance(Vector2 point)
        {
            return Vector2.Distance(lastScreenPos, point);
        }

        public float GetLastScaledDistance(Vector2 point)
        {
            return GetLastScreenDistance(point) * DTouch.ScalingFactor;
        }

        public float GetStartScreenDistance(Vector2 point)
        {
            return Vector2.Distance(startScreenPos, point);
        }

        public float GetStartScaledDistance(Vector2 point)
        {
            return GetStartScreenDistance(point) * DTouch.ScalingFactor;
        }

        public Vector3 GetStartWorldPos(float distance, Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if(cam != null)
            {
                var point = new Vector3(startScreenPos.x, startScreenPos.y, distance);

                return cam.ScreenToWorldPoint(point);
            }
            else
            {
                Debug.LogError("Failed to find camera");
            }

            return default(Vector3);
        }

        public Vector3 GetLastWorldPos(float distance, Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if(cam != null)
            {
                var point = new Vector3(lastScreenPos.x, lastScreenPos.y, distance);

                return cam.ScreenToWorldPoint(point);
            }
            else
            {
                Debug.LogError("Failed to find camera");
            }

            return default(Vector3);
        }

        public Vector3 GetWorldPos(float distance, Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if (cam != null)
            {
                var point = new Vector3(screenPos.x, screenPos.y, distance);

                return cam.ScreenToWorldPoint(point);
            }
            else
            {
                Debug.LogError("Failed to find camera");
            }

            return default(Vector3);
        }

        public Vector3 GetWorldDelta(float distance, Camera cam = null)
        {
            return GetWorldDelta(distance, distance, cam);
        }

        public Vector3 GetWorldDelta(float lastDistance, float distance, Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if(cam != null)
            {
                return GetWorldPos(distance, cam) - GetLastWorldPos(lastDistance, cam);
            }
            else
            {
                Debug.LogError("Failed to find camera");
            }

            return default(Vector3);
        }

        public void ClearSnapshots(int count = -1)
        {
            if(count > 0 && count <= snapshots.Count)
            {
                for(int i = 0; i < count; i++)
                {
                    DSnapshot.inactiveSnapshots.Add(snapshots[i]);
                }

                snapshots.RemoveRange(0, count);
            }
            else if (count < 0)
            {
                DSnapshot.inactiveSnapshots.AddRange(snapshots);

                snapshots.Clear();
            }
        }

        public void RecordSnapshot()
        {
            var snapshot = DSnapshot.Pop();

            snapshot.age = age;
            snapshot.screenPos = screenPos;

            snapshots.Add(snapshot);
        }

        public Vector2 GetSmoothScreenPos(float t)
        {
            if (snapshots.Count > 0 && set == true)
            {
                var d = snapshots[Mathf.Max(0, snapshots.Count - 4)].screenPos;
                var c = snapshots[Mathf.Max(0, snapshots.Count - 3)].screenPos;
                var b = snapshots[Mathf.Max(0, snapshots.Count - 2)].screenPos;
                var a = snapshots[Mathf.Max(0, snapshots.Count - 1)].screenPos;

                return Hermite(d, c, b, a, t);
            }

            return Vector2.LerpUnclamped(lastScreenPos, screenPos, t);
        }

        private static Vector2 Hermite(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            var mu2 = t * t;
            var mu3 = mu2 * t;
            var x = HermiteInterpolate(a.x, b.x, c.x, d.x, t, mu2, mu3);
            var y = HermiteInterpolate(a.y, b.y, c.y, d.y, t, mu2, mu3);

            return new Vector2(x, y);
        }

        private static float HermiteInterpolate(float x0, float x1, float x2, float x3, float mu, float mu2, float mu3)
        {
            var m0 = (x1 - x0) * 0.5f + (x2 - x1) * 0.5f;
            var m1 = (x2 - x1) * 0.5f + (x3 - x2) * 0.5f;
            var a0 = 2f * mu3 - 3f * mu2 + 1f;
            var a1 = mu3 - 2f * mu2 + mu;
            var a2 = mu3 - mu2;
            var a3 = -2f * mu3 + 3f * mu2;

            return (a0 * x1 + a1 * m0 + a2 * m1 + a3 * x2);
        }

        #endregion methods
    }
}
