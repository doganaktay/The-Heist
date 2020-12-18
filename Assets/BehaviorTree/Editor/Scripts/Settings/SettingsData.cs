using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Archi.BT.Visualizer
{
    public enum IconType
    {
        Failure,
        Success,
        Running
    }

    public class SettingsData : ScriptableObject
    {
        public static Color DefaultColor = new Color32(24, 181, 233, 1);

        public float DimLevel = 127f;
        public Color BorderHighlightColor = Color.green;
        public bool EnableMiniMap = false;
        public bool LastRunTimeStamp = true;
        public Sprite FailureIcon = null;
        public Sprite SuccessIcon = null;
        public Sprite RunningIcon = null;

        public NodeProperty DefaultStyleProperties = new NodeProperty();
        public List<NodeProperty> MainStyleProperties = new List<NodeProperty>();
        public List<NodeProperty> OverrideStyleProperties = new List<NodeProperty>();
    }

}
