namespace Archi.BT
{
    public class Repeater : Decorator
    {
        private readonly int repeatCount;

        public Repeater(string name, Node childNode, int repeatCount = 0) : base(name, childNode)
        {
            this.repeatCount = repeatCount;
        }

        protected override void OnReset() { }

        protected override NodeStatus OnRun()
        {
            if (HasNoValidChild())
                return NodeStatus.Failure;

            NodeStatus returnStatus = (ChildNodes[0] as Node).Run();

            if (repeatCount > 0 && repeatCount >= EvaluationCount)
                return NodeStatus.Failure;

            if (returnStatus == NodeStatus.Running)
                return NodeStatus.Success;

            Reset();
            (ChildNodes[0] as Node).Reset();

            return NodeStatus.Success;
        }
    }
}