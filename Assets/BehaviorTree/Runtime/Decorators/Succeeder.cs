namespace Archi.BT
{
    public class Succeeder : Decorator
    {
        // is either running or success, never failure
        public Succeeder(string name, Node childNode) : base(name, childNode) { }

        protected override void OnReset() { }

        protected override NodeStatus OnRun()
        {
            return (ChildNodes[0] as Node).Run() == NodeStatus.Running ? NodeStatus.Running : NodeStatus.Success;
        }
    }
}