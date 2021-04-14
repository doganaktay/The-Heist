namespace Archi.BT
{
    public class SelectorWithFail : Composite
    {
        public SelectorWithFail(string name, params Node[] childNodes) : base(name, childNodes) { }

        protected override void OnReset()
        {
            currentChildIndex = 0;

            for (int i = 0; i < ChildNodes.Count; i++)
                (ChildNodes[i] as Node).Reset();
        }

        protected override NodeStatus OnRun()
        {
            if (HasNoValidChild() || currentChildIndex >= ChildNodes.Count)
                return NodeStatus.Failure;

            NodeStatus childStatus = (ChildNodes[currentChildIndex] as Node).Run();

            switch (childStatus)
            {
                case NodeStatus.Failure:
                    currentChildIndex++;
                    break;
                case NodeStatus.Success:
                    return NodeStatus.Success;
            }

            return NodeStatus.Running;
        }
    }
}