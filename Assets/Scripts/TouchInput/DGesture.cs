using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.Common;

namespace Archi.Touch
{
    public static class DGesture
    {
        public static Vector2 GetScreenCenter()
        {
            return GetScreenCenter(DTouch.fingers);
        }

        public static Vector2 GetScreenCenter(List<DFinger> fingers)
        {
            var center = default(Vector2);
            TryGetScreenCenter(fingers, ref center);
            return center;
        }

        public static bool TryGetScreenCenter(List<DFinger> fingers, ref Vector2 center)
        {
            if(fingers != null)
            {
                var total = Vector2.zero;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    total += fingers[i].screenPos;
                    count++;
                }

                if(count > 0)
                {
                    center = total / count;
                    return true;
                }
            }

            return false;
        }

        public static Vector2 GetLastScreenCenter()
        {
            return GetLastScreenCenter(DTouch.fingers);
        }

        public static Vector2 GetLastScreenCenter(List<DFinger> fingers)
        {
            var center = default(Vector2);
            TryGetLastScreenCenter(fingers, ref center);
            return center;
        }

        public static bool TryGetLastScreenCenter(List<DFinger> fingers, ref Vector2 center)
        {
            if(fingers != null)
            {
                var total = Vector2.zero;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    total += fingers[i].lastScreenPos;
                    count++;
                }

                if(count > 0)
                {
                    center = total / count;
                    return true;
                }
            }

            return false;
        }

        public static Vector2 GetStartScreenCenter()
        {
            return GetStartScreenCenter(DTouch.fingers);
        }

        public static Vector2 GetStartScreenCenter(List<DFinger> fingers)
        {
            var center = default(Vector2);
            TryGetStartScreenCenter(fingers, ref center);
            return center;
        }

        public static bool TryGetStartScreenCenter(List<DFinger> fingers, ref Vector2 center)
        {
            if(fingers != null)
            {
                var total = Vector2.zero;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    total += fingers[i].startScreenPos;
                    count++;
                }

                if(count > 0)
                {
                    center = total / count;
                    return true;
                }
            }

            return false;
        }

        public static Vector2 GetScreenDelta()
        {
            return GetScreenDelta(DTouch.fingers);
        }

        public static Vector2 GetScreenDelta(List<DFinger> fingers)
        {
            var delta = default(Vector2);
            TryGetScreenDelta(fingers, ref delta);
            return delta;
        }

        public static bool TryGetScreenDelta(List<DFinger> fingers, ref Vector2 delta)
        {
            if(fingers != null)
            {
                var total = Vector2.zero;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    var finger = fingers[i];

                    if(finger != null)
                    {
                        total += finger.ScreenDelta;
                        count++;
                    }
                }

                if(count > 0)
                {
                    delta = total / count;
                    return true;
                }
            }

            return false;
        }

        public static Vector2 GetScaledDelta()
        {
            return GetScreenDelta() * DTouch.ScalingFactor;
        }

        public static Vector2 GetScaledDelta(List<DFinger> fingers)
        {
            return GetScreenDelta(fingers) * DTouch.ScalingFactor;
        }

        public static bool TryGetScaledDelta(List<DFinger> fingers, ref Vector2 delta)
        {
            if (TryGetScreenDelta(fingers, ref delta))
            {
                delta *= DTouch.ScalingFactor;
                return true;
            }

            return false;
        }

        public static Vector3 GetWorldDelta(float distance, Camera cam = null)
        {
            return GetWorldDelta(DTouch.fingers, distance, cam);
        }

        public static Vector3 GetWorldDelta(List<DFinger> fingers, float distance, Camera cam = null)
        {
            var delta = default(Vector3);
            TryGetWorldDelta(fingers, distance, ref delta, cam);
            return delta;
        }

        public static bool TryGetWorldDelta(List<DFinger> fingers, float distance, ref Vector3 delta, Camera cam = null)
        {
            if(fingers != null)
            {
                var total = Vector3.zero;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    var finger = fingers[i];

                    if(finger != null)
                    {
                        total += finger.GetWorldDelta(distance, cam);
                        count++;
                    }
                }

                if(count > 0)
                {
                    delta = total / count;
                    return true;
                }
            }

            return false;
        }

        public static float GetScreenDistance()
        {
            return GetScreenDistance(DTouch.fingers);
        }

        public static float GetScreenDistance(List<DFinger> fingers)
        {
            var distance = default(float);
            var center = default(Vector2);

            if(TryGetScreenCenter(fingers, ref center))
            {
                TryGetScreenDistance(fingers, center, ref distance);
            }

            return distance;
        }

        public static float GetScreenDistance(List<DFinger> fingers, Vector2 center)
        {
            var distance = default(float);
            TryGetScreenDistance(fingers, center, ref distance);
            return distance;
        }

        public static bool TryGetScreenDistance(List<DFinger> fingers, Vector2 center, ref float distance)
        {
            if(fingers != null)
            {
                var total = 0f;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    var finger = fingers[i];

                    if(finger != null)
                    {
                        total += finger.GetScreenDistance(center);
                        count++;
                    }
                }

                if(count > 0)
                {
                    distance = total / count;
                    return true;
                }
            }

            return false;
        }

        public static float GetScaledDistance()
        {
            return GetScreenDistance() * DTouch.ScalingFactor;
        }

        public static float GetScaledDistance(List<DFinger> fingers)
        {
            return GetScreenDistance(fingers) * DTouch.ScalingFactor;
        }

        public static float GetScaledDistance(List<DFinger> fingers, Vector2 center)
        {
            return GetScreenDistance(fingers, center) * DTouch.ScalingFactor;
        }

        public static bool TryGetScaledDistance(List<DFinger> fingers, Vector2 center, ref float distance)
        {
            if(TryGetScreenDistance(fingers, center, ref distance))
            {
                distance *= DTouch.ScalingFactor;
                return true;
            }

            return false;
        }

        public static float GetLastScreenDistance()
        {
            return GetLastScreenDistance(DTouch.fingers);
        }

        public static float GetLastScreenDistance(List<DFinger> fingers)
        {
            var distance = default(float);
            var center = default(Vector2);

            if(TryGetLastScreenCenter(fingers, ref center))
                TryGetLastScreenDistance(fingers, center, ref distance);
            

            return distance;
        }

        public static float GetLastScreenDistance(List<DFinger> fingers, Vector2 center)
        {
            var distance = default(float);
            TryGetLastScreenDistance(fingers, center, ref distance);
            return distance;
        }

        public static bool TryGetLastScreenDistance(List<DFinger> fingers, Vector2 center, ref float distance)
        {
            if(fingers != null)
            {
                var total = 0f;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    var finger = fingers[i];

                    if(finger != null)
                    {
                        total += finger.GetLastScreenDistance(center);
                        count++;
                    }
                }

                if(count > 0)
                {
                    distance = total / count;
                    return true;
                }
            }

            return false;
        }

        public static float GetLastScaledDistance()
        {
            return GetLastScreenDistance() * DTouch.ScalingFactor;
        }

        public static float GetLastScaledDistance(List<DFinger> fingers)
        {
            return GetLastScreenDistance(fingers) * DTouch.ScalingFactor;
        }

        public static float GetLastScaledDistance(List<DFinger> fingers, Vector2 center)
        {
            return GetLastScreenDistance(fingers, center) * DTouch.ScalingFactor;
        }

        public static bool TryGetLastScaledDistance(List<DFinger> fingers, Vector2 center, ref float distance)
        {
            if(TryGetLastScreenDistance(fingers, center, ref distance))
            {
                distance *= DTouch.ScalingFactor;
                return true;
            }

            return false;
        }

        public static float GetStartScreenDistance()
        {
            return GetStartScreenDistance(DTouch.fingers);
        }

        public static float GetStartScreenDistance(List<DFinger> fingers)
        {
            var distance = default(float);
            var center = default(Vector2);

            if(TryGetStartScreenCenter(fingers, ref center))
            {
                TryGetStartScreenDistance(fingers, center, ref distance);
            }

            return distance;
        }

        public static float GetStartScreenDistance(List<DFinger> fingers, Vector2 center)
        {
            var distance = default(float);
            TryGetStartScreenDistance(fingers, center, ref distance);
            return distance;
        }

        public static bool TryGetStartScreenDistance(List<DFinger> fingers, Vector2 center, ref float distance)
        {
            if(fingers != null)
            {
                var total = 0f;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    var finger = fingers[i];

                    if(finger != null)
                    {
                        total += finger.GetStartScreenDistance(center);
                        count++;
                    }
                }

                if(count > 0)
                {
                    distance = total / count;
                    return true;
                }
            }

            return false;
        }

        public static float GetStartScaledDistance()
        {
            return GetStartScreenDistance() * DTouch.ScalingFactor;
        }

        public static float GetStartScaledDistance(List<DFinger> fingers)
        {
            return GetStartScreenDistance(fingers) * DTouch.ScalingFactor;
        }

        public static float GetStartScaledDistance(List<DFinger> fingers, Vector2 center)
        {
            return GetStartScreenDistance(fingers, center) * DTouch.ScalingFactor;
        }

        public static bool TryGetStartScaledDistance(List<DFinger> fingers, Vector2 center, ref float distance)
        {
            if(TryGetStartScreenDistance(fingers, center, ref distance))
            {
                distance *= DTouch.ScalingFactor;
                return true;
            }

            return false;
        }

        public static float GetPinchScale()
        {
            return GetPinchScale(DTouch.fingers);
        }

        public static float GetPinchScale(List<DFinger> fingers)
        {
            var scale = 1f;
            var center = GetScreenCenter(fingers);
            var lastCenter = GetLastScreenCenter(fingers);

            TryGetPinchScale(fingers, center, lastCenter, ref scale);

            return scale;
        }

        public static bool TryGetPinchScale(List<DFinger> fingers, Vector2 center, Vector2 lastCenter, ref float scale)
        {
            var distance = GetScreenDistance(fingers, center);
            var lastDistance = GetLastScreenDistance(fingers, center);

            if(lastDistance > 0f)
            {
                scale = distance / lastDistance;
                return true;
            }

            return false;
        }

        public static float GetPinchRatio()
        {
            return GetPinchRatio(DTouch.fingers);
        }

        public static float GetPinchRatio(List<DFinger> fingers)
        {
            var ratio = 1f;
            var center = GetScreenCenter(fingers);
            var lastCenter = GetLastScreenCenter(fingers);

            TryGetPinchRatio(fingers, center, lastCenter, ref ratio);

            return ratio;
        }

        public static bool TryGetPinchRatio(List<DFinger> fingers, Vector2 center, Vector2 lastCenter, ref float ratio)
        {
            var distance = GetScreenDistance(fingers, center);
            var lastDistance = GetLastScreenDistance(fingers, center);

            if(distance > 0f)
            {
                ratio = lastDistance / distance;
                return true;
            }

            return false;
        }

        public static float GetTwistDegrees()
        {
            return GetTwistDegrees(DTouch.fingers);
        }

        public static float GetTwistDegrees(List<DFinger> fingers)
        {
            return GetTwistRadians(fingers) * Mathf.Rad2Deg;
        }

        public static float GetTwistDegrees(List<DFinger> fingers, Vector2 center, Vector2 lastCenter)
        {
            return GetTwistRadians(fingers, center, lastCenter) * Mathf.Rad2Deg;
        }

        public static bool TryGetTwistDegrees(List<DFinger> fingers, Vector2 center, Vector2 lastCenter, ref float degrees)
        {
            if(TryGetTwistRadians(fingers, center, lastCenter, ref degrees))
            {
                degrees *= Mathf.Rad2Deg;
                return true;
            }

            return false;
        }

        public static float GetTwistRadians()
        {
            return GetTwistRadians(DTouch.fingers);
        }

        public static float GetTwistRadians(List<DFinger> fingers)
        {
            var center = GetScreenCenter(fingers);
            var lastCenter = GetLastScreenCenter(fingers);

            return GetTwistRadians(fingers, center, lastCenter);
        }

        public static float GetTwistRadians(List<DFinger> fingers, Vector2 center, Vector2 lastCenter)
        {
            var radians = default(float);
            TryGetTwistRadians(fingers, center, lastCenter, ref radians);
            return radians;
        }

        public static bool TryGetTwistRadians(List<DFinger> fingers, Vector2 center, Vector2 lastCenter, ref float radians)
        {
            if(fingers != null)
            {
                var total = 0f;
                var count = 0;

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    var finger = fingers[i];

                    if(finger != null)
                    {
                        total += finger.GetDeltaRadians(center, lastCenter);
                        count++;
                    }
                }

                if(count > 0)
                {
                    radians = total / count;
                    return true;
                }
            }

            return false;
        }
    }
}
