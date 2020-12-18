using System;
using UnityEngine;
using UnityEditor;

namespace Archi.BT.Visualizer
{
    [Serializable]
    public class NodeProperty
    {
        public MonoScript Script = null;
        public Color TitleBarColor = SettingsData.DefaultColor;
        public Sprite Icon = null;
        public bool IsDecorator = false;
        public bool InvertResult = false;
    }
}