using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.BT.Visualizer
{
    [Serializable]
    public class BTGContainer : ScriptableObject
    {
        public List<BTGNodeLinkData> NodeLinks = new List<BTGNodeLinkData>();
        public List<BTGNodeData> BehaviorTreeNodeData = new List<BTGNodeData>();
    }
}