using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.BT
{
    [System.Serializable]
    public abstract class Node : NodeBase
    {
        public int EvaluationCount { get; private set; }
        public bool DebugNodeStatus = false;
        public bool IsFirstEvalution => EvaluationCount == 0;
        private string lastStatusReason { get; set; } = "";

        // Wrapper for OnRun that keeps track of evaluation count
        // and also raises a status change event when necessary
        public virtual NodeStatus Run()
        {
            NodeStatus nodeStatus = OnRun();

            if(LastNodeStatus != nodeStatus || lastStatusReason.Equals(StatusReason))
            {
                LastNodeStatus = nodeStatus;
                lastStatusReason = StatusReason;
                OnNodeStatusChanged(this);
            }

            EvaluationCount++;

            if (nodeStatus != NodeStatus.Running)
                Reset();

            return nodeStatus;
        }

        public void Reset()
        {
            EvaluationCount = 0;
            OnReset();
        }

        // these need to implement actual node logic per node type
        protected abstract NodeStatus OnRun();
        protected abstract void OnReset();

        #region Helper methods

        protected bool HasNoValidChild()
        {
            var hasNoValidChild = ChildNodes.Count == 0 || ChildNodes[0] == null;

#if UNITY_EDITOR
            if(hasNoValidChild)
                Debug.Log($"Node {Name} has no valid child");
#endif

            return hasNoValidChild;
        }
        
        #endregion
    }

}
