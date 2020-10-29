using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace Archi.Touch
{
    [DisallowMultipleComponent]
    public class DSelectable : MonoBehaviour
    {
        [System.Serializable] public class DFingerEvent : UnityEvent<DFinger> { }
        public static LinkedList<DSelectable> instances = new LinkedList<DSelectable>();
        public static event System.Action<DSelectable, DFinger> OnSelectGlobal;
        public static event System.Action<DSelectable, DFinger> OnSelectSetGlobal;
        public static event System.Action<DSelectable, DFinger> OnSelectUpGlobal;
        public static event System.Action<DSelectable> OnDeselectGlobal;

        public bool deselectOnUp;
        public bool hideWithFinger;
        public bool isolateSelectingFingers;

        public DFingerEvent OnSelect
        {
            get
            {
                if (onSelect == null)
                    onSelect = new DFingerEvent();
                return onSelect;
            }
        }
        [FSA("OnSelect")] [SerializeField] private DFingerEvent onSelect;

        public DFingerEvent OnSelectUpdate
        {
            get
            {
                if (onSelectUpdate == null)
                    onSelectUpdate = new DFingerEvent();
                return onSelectUpdate;
            }
        }
        [FSA("onSelectSet")] [FSA("OnSelectSet")] [SerializeField] private DFingerEvent onSelectUpdate;

        public DFingerEvent OnSelectUp
        {
            get
            {
                if (onSelectUp == null)
                    onSelectUp = new DFingerEvent();
                return OnSelectUp;
            }
        }
        [FSA("OnSelectUp")] [SerializeField] private DFingerEvent onSelectUp;

        public UnityEvent OnDeselect
        {
            get
            {
                if (onDeselect == null)
                    onDeselect = new UnityEvent();
                return onDeselect;
            }
        }
        [FSA("OnDeselect")] [SerializeField] private UnityEvent onDeselect;

        [SerializeField] private bool isSelected;
        [System.NonSerialized] private List<DFinger> selectingFingers = new List<DFinger>();
        [System.NonSerialized] private LinkedListNode<DSelectable> node;

        public bool IsSelected
        {
            set
            {
                if (value == true)
                    Select();
                else
                    Deselect();
            }
            get
            {
                if (hideWithFinger && isSelected && selectingFingers.Count > 0)
                    return false;

                return isSelected;
            }
        }

        public bool IsSelectedRaw
        {
            get { return isSelected; }
        }

        public static int IsSelectedCount
        {
            get
            {
                var count = 0;
                foreach(var selectable in instances)
                {
                    if (selectable.IsSelected)
                        count++;
                }

                return count;
            }
        }

        public DFinger SelectingFinger
        {
            get
            {
                if (selectingFingers.Count > 0)
                    return selectingFingers[0];

                return null;
            }
        }

        public List<DFinger> SelectingFingers
        {
            get
            {
                return selectingFingers;
            }
        }

        public static List<DFinger> GetFingers(bool ignoreIfStartedOverGUI, bool ignoreIfOverGUI, int requiredFingerCount = 0, DSelectable requiredSelectable = null)
        {
            var fingers = DTouch.GetFingers(ignoreIfStartedOverGUI, ignoreIfOverGUI, requiredFingerCount);

            if(requiredSelectable != null)
            {
                if (!requiredSelectable.isSelected)
                    fingers.Clear();

                if (requiredSelectable.isolateSelectingFingers)
                {
                    fingers.Clear();
                    fingers.AddRange(requiredSelectable.selectingFingers);

                    if (requiredFingerCount > 0 && fingers.Count != requiredFingerCount)
                        fingers.Clear();
                }
            }

            return fingers;
        }

        private static List<DSelectable> tempSelectables = new List<DSelectable>();

        public static void GetSelected(List<DSelectable> list)
        {
            if(list != null)
            {
                list.Clear();

                foreach(var selectable in instances)
                {
                    if (selectable.isSelected)
                        list.Add(selectable);
                }
            }
        }

        public static void Cull(int maxCount)
        {
            GetSelected(tempSelectables);

            for(int i = maxCount; i < tempSelectables.Count; i++)
            {
                var selectable = tempSelectables[i];

                if (selectable != null)
                    selectable.Deselect();
            }
        }

        public static DSelectable FindSelectable(DFinger finger)
        {
            foreach(var selectable in instances)
            {
                if (selectable.IsSelectedBy(finger))
                    return selectable;
            }

            return null;
        }

        public static void ReplaceSelection(DFinger finger, List<DSelectable> selectables)
        {
            var selectableCount = 0;

            if(selectables != null)
            {
                tempSelectables.Clear();

                foreach(var selectable in instances)
                {
                    if (selectable.isSelected && !selectables.Contains(selectable))
                        tempSelectables.Add(selectable);
                }

                foreach(var selectable in instances)
                {
                    if (selectable != null)
                        selectable.Deselect();
                }
            }

            if(selectables != null)
            {
                for(int i = selectables.Count - 1; i >= 0; i--)
                {
                    var selectable = selectables[i];

                    if(selectable != null)
                    {
                        if (!selectable.isSelected)
                            selectable.Select(finger);

                        selectableCount++;
                    }
                }
            }

            if (selectableCount == 0)
                DeselectAll();
        }

        public bool IsSelectedBy(DFinger finger)
        {
            for(int i = selectingFingers.Count - 1; i >= 0; i--)
            {
                if (selectingFingers[i] == finger)
                    return true;
            }

            return false;
        }

        public bool GetIsSelected(bool raw)
        {
            return raw ? IsSelectedRaw : IsSelected;
        }

        [ContextMenu("Select")]
        public void Select()
        {
            Select(null);
        }

        public void Select(DFinger finger)
        {
            isSelected = true;

            if(finger != null)
            {
                if (!IsSelectedBy(finger))
                    selectingFingers.Add(finger);
            }

            if (onSelect != null)
                onSelect.Invoke(finger);

            if (OnSelectGlobal != null)
                OnSelectGlobal(this, finger);
        }

        [ContextMenu("Deselect")]
        public void Deselect()
        {
            if (isSelected)
            {
                isSelected = false;

                for(int i = selectingFingers.Count - 1; i >= 0; i--)
                {
                    var selectingFinger = selectingFingers[i];

                    if(selectingFinger != null)
                    {
                        if (onSelectUp != null)
                            onSelectUp.Invoke(selectingFinger);

                        if (OnSelectUpGlobal != null)
                            OnSelectUpGlobal(this, selectingFinger);
                    }
                }

                selectingFingers.Clear();

                if (onDeselect != null)
                    onDeselect.Invoke();

                if (OnDeselectGlobal != null)
                    OnDeselectGlobal(this);
            }
        }

        public static void DeselectAll()
        {
            GetSelected(tempSelectables);

            foreach(var selectable in tempSelectables)
            {
                selectable.Deselect();
            }
        }

        protected virtual void OnEnable()
        {
            instances.AddLast(this);

            if(instances.Count == 1)
            {
                DTouch.OnFingerUpdate += HandleFingerUpdate;
                DTouch.OnFingerUp += HandleFingerUp;
                DTouch.OnFingerInactive += HandleFingerInactive;
            }
        }

        protected virtual void OnDisable()
        {
            if (instances.Count == 1)
            {
                DTouch.OnFingerUpdate -= HandleFingerUpdate;
                DTouch.OnFingerUp -= HandleFingerUp;
                DTouch.OnFingerInactive -= HandleFingerInactive;
            }

            instances.Remove(this);

            if (isSelected)
                Deselect();
        }

        private static void BuildTempSelectables(DFinger finger)
        {
            tempSelectables.Clear();

            foreach(var selectable in instances)
            {
                if (selectable.IsSelectedBy(finger))
                    tempSelectables.Add(selectable);
            }
        }

        private static void HandleFingerUpdate(DFinger finger)
        {
            BuildTempSelectables(finger);

            foreach(var selectable in tempSelectables)
            {
                if(selectable != null)
                {
                    if (selectable.onSelectUpdate != null)
                        selectable.onSelectUpdate.Invoke(finger);

                    if (OnSelectGlobal != null)
                        OnSelectGlobal(selectable, finger);
                }
            }
        }

        private bool AnyFingersSet
        {
            get
            {
                for(var i = selectingFingers.Count - 1; i >= 0; i--)
                {
                    if (selectingFingers[i].set)
                        return true;
                }

                return false;
            }
        }

        private static void HandleFingerUp(DFinger finger)
        {
            BuildTempSelectables(finger);

            foreach(var selectable in tempSelectables)
            {
                if(selectable != null)
                {
                    if (selectable.deselectOnUp && selectable.isSelected && !selectable.AnyFingersSet)
                        selectable.Deselect();
                    else
                    {
                        if (selectable.onSelectUp != null)
                            selectable.onSelectUp.Invoke(finger);

                        if (OnSelectUpGlobal != null)
                            OnSelectUpGlobal(selectable, finger);
                    }
                }
            }
        }

        private static void HandleFingerInactive(DFinger finger)
        {
            foreach (var selectable in instances)
                selectable.selectingFingers.Remove(finger);
        }
    }
}

#if UNITY_EDITOR

namespace Archi.Touch.Inspector
{
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(DSelectable))]
    public class DSelectable_Inspector : Archi.Common.DInspector<DSelectable>
    {
        private bool showUnusedEvents;

        protected override void DrawInspector()
        {
            if(Draw("isSelected", "Is this LeanSelectable component currently selected?"))
            {
                var isSelected = serializedObject.FindProperty("isSelected").boolValue;

                Each(t => t.IsSelected = isSelected);
            }

            Draw("deselectOnUp", "Should this get deselected when the selecting finger goes up?");
            Draw("hideWithFinger", "Should IsSelected temporarily return false if the selecting finger is still being held? This is useful when selecting multiple objects using a complex gesture (e.g. RTS style selection box).");
            Draw("isolateSelectingFingers", "If the selecting fingers are still active, only return those to RequiredSelectable queries?");

            EditorGUILayout.Separator();

            var usedA = Any(t => t.OnSelect.GetPersistentEventCount() > 0);
            var usedB = Any(t => t.OnSelectUpdate.GetPersistentEventCount() > 0);
            var usedC = Any(t => t.OnSelectUp.GetPersistentEventCount() > 0);
            var usedD = Any(t => t.OnDeselect.GetPersistentEventCount() > 0);

            showUnusedEvents = EditorGUILayout.Foldout(showUnusedEvents, "Show Unused Events");

            EditorGUILayout.Separator();

            if (usedA || showUnusedEvents)
                Draw("onSelect");
            if (usedB || showUnusedEvents)
                Draw("onSelectUpdate");
            if (usedC || showUnusedEvents)
                Draw("onSelectUp");
            if (usedD || showUnusedEvents)
                Draw("onDeselect");
        }
    }
}

#endif
