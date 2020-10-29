// the Archi.Touch namespace is a rewrite of Lean Touch (for practice)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.Touch
{
    public class DSnapshot
    {
        public float age;
        public Vector2 screenPos;

        public Vector3 GetWorldPos(float distance, Camera cam = null)
        {
            cam = DTouch.GetCamera(cam);

            if(cam != null)
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

        public static List<DSnapshot> inactiveSnapshots = new List<DSnapshot>(1000);

        public static DSnapshot Pop()
        {
            if(inactiveSnapshots.Count > 0)
            {
                var index = inactiveSnapshots.Count - 1;
                var snapshot = inactiveSnapshots[index];

                inactiveSnapshots.RemoveAt(index);

                return snapshot;
            }

            return new DSnapshot();
        }

        public static bool TryGetScreenPos(List<DSnapshot> snapshots, float targetAge, ref Vector2 screenPos)
        {
            if(snapshots != null && snapshots.Count > 0)
            {
                var snapshotF = snapshots[0];

                if(targetAge <= snapshotF.age)
                {
                    screenPos = snapshotF.screenPos;
                    return true;
                }

                var snapshotL = snapshots[snapshots.Count - 1];

                if(targetAge >= snapshotL.age)
                {
                    screenPos = snapshotL.screenPos;
                    return true;
                }

                var lowerIndex = GetLowerIndex(snapshots, targetAge);
                var upperIndex = lowerIndex + 1;
                var lower = snapshots[lowerIndex];
                var upper = upperIndex < snapshots.Count ? snapshots[upperIndex] : lower;
                var across = Mathf.InverseLerp(lower.age, upper.age, targetAge);

                screenPos = Vector2.Lerp(lower.screenPos, upper.screenPos, across);

                return true;
            }

            return false;
        }

        public static bool TryGetSnapshot(List<DSnapshot> snapshots, int index, ref float age, ref Vector2 screenPos)
        {
            if(index >= 0 && index < snapshots.Count)
            {
                var snapshot = snapshots[index];

                age = snapshot.age;
                screenPos = snapshot.screenPos;

                return true;
            }

            return true;
        }

        public static int GetLowerIndex(List<DSnapshot> snapshots, float targetAge)
        {
            if(snapshots != null)
            {
                var count = snapshots.Count;

                if(count > 0)
                {
                    for(int i = count - 1; i >= 0; i--)
                    {
                        if (snapshots[i].age <= targetAge)
                            return i;
                    }
                }

                return 0;
            }

            return -1;
        }
    }
}
