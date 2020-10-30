// the Archi.Touch namespace is a rewrite of Lean Touch (for practice)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Archi.Common;

namespace Archi.Touch
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class DTouch : MonoBehaviour
    {
        public static List<DTouch> instances = new List<DTouch>();
        public static List<DFinger> fingers = new List<DFinger>();
        public static List<DFinger> inactiveFingers = new List<DFinger>();

        #region actions

        public static event Action<DFinger> OnFingerDown;
        public static event Action<DFinger> OnFingerUpdate;
        public static event Action<DFinger> OnFingerUp;
        public static event Action<DFinger> OnFingerOld;
        public static event Action<DFinger> OnFingerTap;
        public static event Action<DFinger> OnFingerSwipe;
        public static event Action<List<DFinger>> OnGesture;
        public static event Action<DFinger> OnFingerExpired;
        public static event Action<DFinger> OnFingerInactive;

        #endregion actions

        [Tooltip("Time required between a finger down/up to register a tap")]
        public float tapThreshold = defaultTapThreshold;
        public const float defaultTapThreshold = 0.2f;
        public static float CurrentTapThreshold { get { return instances.Count > 0 ? instances[0].tapThreshold : defaultTapThreshold; } }

        [Tooltip("Move delta required (relative to referenceDpi) within tap threshold to trigger a swipe")]
        public float swipeThreshold = defaultSwipeThreshold;
        public const float defaultSwipeThreshold = 100f;
        public static float CurrentSwipeThreshold { get { return instances.Count > 0 ? instances[0].swipeThreshold : defaultSwipeThreshold; } }

        [Tooltip("Set default DPI to use for input scaling")]
        public int referenceDPI = defaultReferenceDPI;
        public const int defaultReferenceDPI = 200;
        public static int CurrentReferenceDPI { get { return instances.Count > 0 ? instances[0].referenceDPI : defaultReferenceDPI; } }

        [Tooltip("GUI layers")]
        public LayerMask GUILayers = 1 << 5;
        public static LayerMask CurrentGUILayers { get { return instances.Count > 0 ? instances[0].GUILayers : (LayerMask)Physics.DefaultRaycastLayers; } }

        [Tooltip("Should fingers record snapshots of their screen positions?")]
        public bool recordFingers = true;
        [Tooltip("Move delta required to store another snapshot")]
        public float recordThreshold = 5f;
        [Tooltip("Max seconds to record snapshots, 0 = unlimited")]
        public float recordLimit = 10f;

        #region member variables

        private static Vector2 pivot = new Vector2(0.5f, 0.5f);
        private static List<RaycastResult> tempRaycastResults = new List<RaycastResult>(10);
        private static List<DFinger> filteredFingers = new List<DFinger>(10);
        private static PointerEventData tempPointerEventData;
        private static EventSystem tempEventSystem;

        #endregion member variables

        #region getters

        public static DTouch Instance { get { return instances.Count > 0 ? instances[0] : null; } }
        public static float ScalingFactor {
            get
            {
                var dpi = Screen.dpi;

                if (dpi <= 0)
                    return 1f;

                return CurrentReferenceDPI / dpi;
            }
        }
        public static float ScreenFactor
        {
            get
            {
                var size = Mathf.Min(Screen.width, Screen.height);

                if (size <= 0)
                    return 1f;

                return 1f / size;
            }
        }
        public static bool GUIInUse
        {
            get
            {
                for(int i = fingers.Count; i >= 0; i--)
                {
                    if (fingers[i].startedOverGUI)
                        return true;
                }

                return false;
            }
        }

        #endregion getters

        #region methods

        public static Camera GetCamera(Camera currentCam, GameObject go = null)
        {
            if(currentCam == null)
            {
                if(go != null)
                {
                    currentCam = go.GetComponent<Camera>();
                }

                if(currentCam == null)
                {
                    currentCam = Camera.main;
                }
            }

            return currentCam;
        }

        public static float GetDampenFactor(float dampening, float deltaTime)
        {
            if (dampening < 0f)
                return 1f;

            if (Application.isPlaying == false)
                return 1f;

            return 1f - Mathf.Exp(-dampening * deltaTime);
        }

        public static bool PointOverGUI(Vector2 screenPos)
        {
            return RaycastGUI(screenPos).Count > 0;
        }

        public static List<RaycastResult> RaycastGUI(Vector2 screenPos)
        {
            return RaycastGUI(screenPos, CurrentGUILayers);
        }

        public static List<RaycastResult> RaycastGUI(Vector2 screenPos, LayerMask GUImask)
        {
            tempRaycastResults.Clear();

            var currentEventSystem = EventSystem.current;
            if (currentEventSystem == null)
                currentEventSystem = FindObjectOfType<EventSystem>();

            if (currentEventSystem != null)
            {
                if (currentEventSystem != tempEventSystem)
                {
                    tempEventSystem = currentEventSystem;

                    if (tempPointerEventData == null)
                        tempPointerEventData = new PointerEventData(tempEventSystem);
                    else
                        tempPointerEventData.Reset();
                }

                tempPointerEventData.position = screenPos;
                currentEventSystem.RaycastAll(tempPointerEventData, tempRaycastResults);

                if(tempRaycastResults.Count > 0)
                {
                    for(int i = tempRaycastResults.Count - 1; i >= 0; i--)
                    {
                        var result = tempRaycastResults[i];
                        var layer = 1 << result.gameObject.layer;

                        if ((layer & GUImask) == 0)
                            tempRaycastResults.RemoveAt(i);
                    }
                }
            }
            else
            {
                Debug.LogError("No event system found");
            }

            return tempRaycastResults;
        }

        public static List<DFinger> GetFingers(bool ignoreIfStartedOverGUI, bool ignoreIfOverGUI, int requiredFingerCount = 0)
        {
            filteredFingers.Clear();

            for(int i = 0; i < fingers.Count; i++)
            {
                var finger = fingers[i];

                if ((ignoreIfStartedOverGUI && finger.startedOverGUI) || (ignoreIfOverGUI && finger.IsOverGUI))
                    continue;

                filteredFingers.Add(finger);
            }

            if(requiredFingerCount > 0)
            {
                if(filteredFingers.Count != requiredFingerCount)
                {
                    filteredFingers.Clear();
                    return filteredFingers;
                }
            }

            return filteredFingers;
        }

        protected virtual void OnEnable()
        {
            instances.Add(this);
        }

        protected virtual void OnDisable()
        {
            instances.Remove(this);
        }

        protected virtual void Update()
        {
            if(instances[0] == this)
            {
                BeginFingers();
                PollFingers();
                EndFingers();
                UpdateEvents();
            }
        }

        private void BeginFingers()
        {
            // age inactive fingers
            for(int i = inactiveFingers.Count - 1; i >= 0; i--)
            {
                var inactiveFinger = inactiveFingers[i];

                inactiveFinger.age += Time.unscaledDeltaTime;

                if(!inactiveFinger.expired &&  inactiveFinger.age > tapThreshold)
                {
                    inactiveFinger.expired = true;

                    if (OnFingerExpired != null)
                        OnFingerExpired(inactiveFinger);
                }
            }

            // reset finger data
            for(int i = fingers.Count - 1; i >= 0; i--)
            {
                var finger = fingers[i];

                if(finger.Up || !finger.set)
                {
                    fingers.RemoveAt(i);
                    inactiveFingers.Add(finger);

                    finger.age = 0f;
                    finger.ClearSnapshots();

                    if (OnFingerInactive != null)
                        OnFingerInactive(finger);
                }
                else
                {
                    finger.lastSet = finger.set;
                    finger.lastScreenPos = finger.screenPos;

                    finger.set = false;
                    finger.tap = false;
                    finger.swipe = false;
                }
            }
        }

        private void EndFingers()
        {
            for(int i = fingers.Count - 1; i >= 0; i--)
            {
                var finger = fingers[i];

                if (finger.Up)
                {
                    if(finger.age <= tapThreshold)
                    {
                        if(finger.SwipeScreenDelta.magnitude * ScalingFactor < swipeThreshold)
                        {
                            finger.tap = true;
                            finger.tapCount++;
                        }
                        else
                        {
                            finger.tapCount = 0;
                            finger.swipe = true;
                        }
                    }
                    else
                    {
                        finger.tapCount = 0;
                    }
                }
                else if (!finger.Down)
                {
                    finger.age += Time.unscaledDeltaTime;

                    if(finger.age > tapThreshold && !finger.old)
                    {
                        finger.old = true;

                        if (OnFingerOld != null)
                            OnFingerOld(finger);
                    }
                }

            }
        }

        private static HashSet<DFinger> missingFingers = new HashSet<DFinger>();
        private static List<DFinger> tempFingers = new List<DFinger>();

        private void PollFingers()
        {
            missingFingers.Clear();

            for(int i = fingers.Count - 1; i >= 0; i--)
            {
                missingFingers.Add(fingers[i]);
            }

            if(DInput.GetTouchCount() > 0)
            {
                for(int i = 0; i < DInput.GetTouchCount(); i++)
                {
                    int id; Vector2 pos; bool set;

                    DInput.GetTouch(i, out id, out pos, out set);

                    AddFinger(id, pos, set);
                }
            }

            tempFingers.Clear();
            tempFingers.AddRange(missingFingers);
            foreach(var finger in tempFingers)
            {
                AddFinger(finger.index, finger.screenPos, false);
            }
        }

        private void UpdateEvents()
        {
            var count = fingers.Count;

            for(int i = 0; i < count; i++)
            {
                var finger = fingers[i];

                if (finger.tap && OnFingerTap != null) OnFingerTap(finger);
                if (finger.swipe && OnFingerSwipe != null) OnFingerSwipe(finger);
                if (finger.Down  && OnFingerDown != null) OnFingerDown(finger);
                if (OnFingerUpdate != null) OnFingerUpdate(finger);
                if (finger.Up && OnFingerUp != null) OnFingerUp(finger);
            }

            if(OnGesture != null)
            {
                filteredFingers.Clear();
                filteredFingers.AddRange(fingers);

                OnGesture(filteredFingers);
            }
        }

        private DFinger AddFinger(int index, Vector2 screenPos, bool set)
        {
            var finger = FindFinger(index);

            if(finger != null)
            {
                missingFingers.Remove(finger);
            }
            else
            {
                if (!set)
                    return null;

                var inactiveIndex = FindInactiveFingerIndex(index);

                if(inactiveIndex >= 0)
                {
                    finger = inactiveFingers[inactiveIndex];
                    inactiveFingers.RemoveAt(inactiveIndex);

                    if (finger.age > tapThreshold)
                        finger.tapCount = 0;

                    finger.age = 0f;
                    finger.old = false;
                    finger.set = false;
                    finger.lastSet = false;
                    finger.tap = false;
                    finger.swipe = false;
                    finger.expired = false;
                }
                else
                {
                    finger = new DFinger();
                    finger.index = index;
                }

                finger.startScreenPos = screenPos;
                finger.lastScreenPos = screenPos;
                finger.startedOverGUI = PointOverGUI(screenPos);

                fingers.Add(finger);
            }

            finger.set = set;
            finger.screenPos = screenPos;

            if (recordFingers)
            {
                if(recordLimit > 0f)
                {
                    if(finger.SnapshotDuration > recordLimit)
                    {
                        var removeCount = DSnapshot.GetLowerIndex(finger.snapshots, finger.age - recordLimit);
                        finger.ClearSnapshots(removeCount);
                    }
                }

                if(recordThreshold > 0f)
                {
                    if(finger.snapshots.Count == 0 || finger.LastSnapshotScreenDelta.magnitude >= recordThreshold)
                    {
                        finger.RecordSnapshot();
                    }
                }
                else
                {
                    finger.RecordSnapshot();
                }
            }

            return finger;
        }

        private DFinger FindFinger(int index)
        {
            for(int i = fingers.Count - 1; i >= 0; i--)
            {
                var finger = fingers[i];

                if (finger.index == index)
                    return finger;
            }

            return null;
        }

        private int FindInactiveFingerIndex(int index)
        {
            for(int i = inactiveFingers.Count - 1; i >= 0; i--)
            {
                if (inactiveFingers[i].index == index)
                    return i;
            }

            return -1;
        }

        #endregion methods
    }
}

#if UNITY_EDITOR
namespace Archi.Touch.Inspector
{
    using UnityEditor;

    [CustomEditor(typeof(DTouch))]
    public class DTouch_Editor : Editor
    {
        private static List<DFinger> allFingers = new List<DFinger>();
        private static GUIStyle fadingLabel;

        [MenuItem("GameObject/Archi/Touch")]
        public static void CreateTouch()
        {
            var gameObject = new GameObject(typeof(DTouch).Name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Touch");
            gameObject.AddComponent<DTouch>();
            Selection.activeGameObject = gameObject;
        }

        public override void OnInspectorGUI()
        {
            if(DTouch.instances.Count > 1)
            {
                EditorGUILayout.HelpBox("There is more than one active and enabled DTouch", MessageType.Warning);
                EditorGUILayout.Separator();
            }

            var touch = (DTouch)target;

            EditorGUILayout.Separator();
            DrawSettings(touch);
            EditorGUILayout.Separator();
            DrawFingers(touch);
            EditorGUILayout.Separator();
            Repaint();
        }

        private void DrawSettings(DTouch touch)
        {
            DrawDefault("tapThreshold");
            DrawDefault("swipeThreshold");
            DrawDefault("referenceDPI");
            DrawDefault("GUILayers");

            EditorGUILayout.Separator();

            DrawDefault("recordFingers");

            if(touch.recordFingers)
            {
                EditorGUI.indentLevel++;
                DrawDefault("recordThreshold");
                DrawDefault("recordLimit");
            }
        }

        private void DrawFingers(DTouch touch)
        {
            EditorGUILayout.LabelField("Fingers", EditorStyles.boldLabel);

            allFingers.Clear();
            allFingers.AddRange(DTouch.fingers);
            allFingers.AddRange(DTouch.inactiveFingers);
            allFingers.Sort((a, b) => a.index.CompareTo(b.index));

            for(int i = 0; i < allFingers.Count; i++)
            {
                var finger = allFingers[i];
                var progress = touch.tapThreshold > 0f ? finger.age / touch.tapThreshold : 0f;
                var style = GetFadingLabel(finger.set, progress);

                if(style.normal.textColor.a > 0f)
                {
                    var screenPos = finger.screenPos;
                    EditorGUILayout.LabelField("#" + finger.index + " x " + finger.tapCount + " (" +
                        Mathf.FloorToInt(screenPos.x) + ", " + Mathf.FloorToInt(screenPos.y) + ") - age: " +
                        finger.age.ToString("0.0"), style);
                }
            }
        }

        private void DrawDefault(string name)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(name));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static GUIStyle GetFadingLabel(bool active, float progress)
        {
            if(fadingLabel == null)
            {
                fadingLabel = new GUIStyle(EditorStyles.label);
            }

            var a = EditorStyles.label.normal.textColor;
            var b = a; b.a = active == true ? 0.5f : 0f;

            fadingLabel.normal.textColor = Color.Lerp(a, b, progress);

            return fadingLabel;
        }

    }
}
#endif