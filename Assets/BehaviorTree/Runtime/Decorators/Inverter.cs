using UnityEngine;

namespace Archi.BT
{
    public class Inverter : Decorator
    {
        public Inverter(string name, Node childNode) : base(name, childNode) { }

        protected override void OnReset() { }

        protected override NodeStatus OnRun()
        {
            if (HasNoValidChild())
                return NodeStatus.Failure;

            NodeStatus childStatus = (ChildNodes[0] as Node).Run();

            switch (childStatus)
            {
                case NodeStatus.Failure:
                    return NodeStatus.Success;
                case NodeStatus.Success:
                    return NodeStatus.Failure;
            }

#if UNITY_EDITOR
            Debug.Log("Inverter has failed");
#endif
            return childStatus;
        }
    }
}
