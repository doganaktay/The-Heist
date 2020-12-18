namespace Archi.BT
{
    public class UntilFail : Decorator
    {
        public UntilFail(string name, Node childNode) : base(name, childNode) { }

        protected override void OnReset() { }

        protected override NodeStatus OnRun()
        {
            return (ChildNodes[0] as Node).Run() == NodeStatus.Failure ? NodeStatus.Failure : NodeStatus.Running;
        }
    }
}