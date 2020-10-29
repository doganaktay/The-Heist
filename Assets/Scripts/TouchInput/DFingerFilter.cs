using System.Collections.Generic;
using UnityEngine;

namespace Archi.Touch
{
    [System.Serializable]
    public struct DFingerFilter
    {
        public enum FilterType
        {
            AllFingers,
            ManuallyAddedFingers
        }

        public FilterType filter;
        public bool ignoreStartedOverGUI;
        public int requiredFingerCount; // 0 = any amount
        public DSelectable requiredSelectable;

        [System.NonSerialized]
        private List<DFinger> fingers;

        public DFingerFilter(bool newIgnoreStartedOverGUI) : this(default(FilterType), newIgnoreStartedOverGUI, default(int), default(DSelectable))
        {
        }

        public DFingerFilter(FilterType newFilter, bool newIgnoreStartedOverGUI, int newRequiredFingerCount, DSelectable newRequiredSelectable)
        {
            filter = newFilter;
            ignoreStartedOverGUI = newIgnoreStartedOverGUI;
            requiredFingerCount = newRequiredFingerCount;
            requiredSelectable = newRequiredSelectable;

            fingers = null;
        }

        public void UpdateRequiredSelectable(GameObject go)
        {
            if (requiredSelectable == null && go != null)
                requiredSelectable = go.GetComponentInParent<DSelectable>();
        }

        public void AddFinger(DFinger finger)
        {
            if(filter == FilterType.ManuallyAddedFingers && finger != null)
            {
                if (fingers == null)
                    fingers = new List<DFinger>();

                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    if (fingers[i] == finger)
                        return;
                }

                if (fingers.Count == 0)
                    DTouch.OnFingerUp += RemoveFinger;

                fingers.Add(finger);
            }
        }

        public void RemoveFinger(DFinger finger)
        {
            for(int i = fingers.Count - 1; i >= 0; i--)
            {
                if(fingers[i] == finger)
                {
                    fingers.RemoveAt(i);

                    if (fingers.Count == 0)
                        DTouch.OnFingerUp -= RemoveFinger;

                    return;
                }
            }
        }

        public void RemoveAllFingers()
        {
            if(fingers != null)
            {
                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    RemoveFinger(fingers[i]);
                }
            }
        }

        public List<DFinger> GetFingers(bool ignoreUpFingers = false)
        {
            if (fingers == null)
                fingers = new List<DFinger>();

            switch (filter)
            {
                case FilterType.AllFingers:
                    {
                        fingers.Clear();
                        fingers.AddRange(DSelectable.GetFingers(ignoreStartedOverGUI, false, requiredFingerCount, requiredSelectable));
                    }
                    break;
            }

            if (ignoreUpFingers)
            {
                for(int i = fingers.Count - 1; i >= 0; i--)
                {
                    if (fingers[i].Up)
                        fingers.RemoveAt(i);
                }
            }

            return fingers;
        }
    }
}

#if UNITY_EDITOR

namespace Archi.Touch
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(DFingerFilter))]
    public class DFingerFilter_Drawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var filter = (DFingerFilter.FilterType)property.FindPropertyRelative("filter").enumValueIndex;
            var height = base.GetPropertyHeight(property, label);
            var step = height + 2;

            switch (filter)
            {
                case DFingerFilter.FilterType.AllFingers: height += step * 4; break;
            }

            return height;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var filter = (DFingerFilter.FilterType)property.FindPropertyRelative("filter").enumValueIndex;
            var height = base.GetPropertyHeight(property, label);

            rect.height = height;

            DrawProperty(ref rect, property, label, "filter", label.text, "The method used to find fingers to use with this component.\n\nManuallyAddedFingers = You must manually call the AddFinger function (e.g. from a UI button).");

            EditorGUI.indentLevel++;
            {
                switch (filter)
                {
                    case DFingerFilter.FilterType.AllFingers:
                        {
                            DrawProperty(ref rect, property, label, "requiredSelectable", null, "If the specified RequiredSelectable component's IsSelected setting is false, ignore all fingers?");
                            DrawProperty(ref rect, property, label, "requiredFingerCount", null, "If the amount of fingers doesn't match this number, ignore all fingers?\n\n0 = Any amount.");
                            DrawProperty(ref rect, property, label, "requiredMouseButtons", null, "When using simulated fingers, should a specific combination of mouse buttons be held?\n\n0 = Any.\n1 = Left.\n2 = Right.\n3 = Left + Right.\n4 = Middle.\n5 = Left + Middle.\n6 = Right + Middle.\n7 = Left + Right + Middle.");
                            DrawProperty(ref rect, property, label, "ignoreStartedOverGui", null, "Ignore fingers that began touching the screen on top of a GUI element?");
                        }
                        break;
                }
            }
                EditorGUI.indentLevel--;
        }

        private void DrawProperty(ref Rect rect, SerializedProperty property, GUIContent label, string childName, string overrideName = null, string overrideTooltip = null)
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