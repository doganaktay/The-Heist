using UnityEngine;

namespace Archi.BT
{
    public class Timer : Decorator
    {
        private float startTime;
        private bool useFixedTime;
        private float timeToWait;

        public Timer(float timeToWait, Node childNode, bool useFixedTime = false) : base($"{timeToWait} second timer", childNode)
        {
            this.useFixedTime = useFixedTime;
            this.timeToWait = timeToWait;
        }

        protected override void OnReset() { }

        protected override NodeStatus OnRun()
        {
            if (HasNoValidChild())
                return NodeStatus.Failure;

            NodeStatus childStatus = (ChildNodes[0] as Node).Run();
            float elapsedTime = Time.fixedTime - startTime;

            if (IsFirstEvalution)
            {
                StatusReason = $"Starting timer for {timeToWait}. Child node status: {childStatus}";
                startTime = useFixedTime ? Time.fixedTime : Time.time;
            }
            else if (elapsedTime > timeToWait)
            {
                StatusReason = $"Timer complete. Child node status: {childStatus}";
                return NodeStatus.Success;
            }

            StatusReason = $"Time remaining {timeToWait - elapsedTime}. Child node status: {childStatus}";
            return NodeStatus.Running;
        }
    }
}