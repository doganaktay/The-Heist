using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.BT
{
    public delegate void NodeStatusChangedHandler(NodeBase sender);

    public enum NodeStatus
    {
        Failure,
        Success,
        Running,
        Unknown,
        NotRun
    }

    public class NodeBase
    {
        public string Name { get; set; }
        public string StatusReason { get; set; } = "";
        public List<NodeBase> ChildNodes = new List<NodeBase>();
        public NodeStatus LastNodeStatus = NodeStatus.NotRun;

        public event NodeStatusChangedHandler NodeStatusChanged;

        protected virtual void OnNodeStatusChanged(NodeBase sender)
        {
            NodeStatusChanged?.Invoke(sender);
        }
    }

}
