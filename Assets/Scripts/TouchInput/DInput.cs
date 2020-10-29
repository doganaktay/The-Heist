// the Archi.Touch namespace is a rewrite of Lean Touch (for practice)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.Common
{
    public static class DInput
    {
        public static int GetTouchCount()
        {
            return Input.touchCount;
        }

        public static void GetTouch(int index, out int id, out Vector2 pos, out bool set)
        {
            var touch = Input.GetTouch(index);

            id = touch.fingerId;
            pos = touch.position;
            set = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved;
        }
    }

}
