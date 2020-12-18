namespace Archi.BT
{
    public class Sequence : Composite
    {
        public Sequence(string name, params Node[] childNodes) : base(name, childNodes) { }

        protected override void OnReset()
        {
            currentChildIndex = 0;

            for (int i = 0; i < ChildNodes.Count; i++)
                (ChildNodes[i] as Node).Reset();
        }

        protected override NodeStatus OnRun()
        {
            NodeStatus childStatus = (ChildNodes[currentChildIndex] as Node).Run();

            switch (childStatus)
            {
                case NodeStatus.Failure:
                    return childStatus;
                case NodeStatus.Success:
                    currentChildIndex++;
                    break;
            }

            if (currentChildIndex >= ChildNodes.Count)
                return NodeStatus.Success;

            return childStatus == NodeStatus.Success ? OnRun() : NodeStatus.Running;
        }
    }
}