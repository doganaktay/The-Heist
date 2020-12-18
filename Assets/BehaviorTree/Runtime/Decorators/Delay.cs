using UnityEngine;

namespace Archi.BT
{
    public class Delay : Decorator
    {
        private float startTime;
        private bool useFixedTime;
        private float timeToWait;

        public Delay(float timeToWait, Node childNode, bool useFixedTime = false) : base($"Runs after {timeToWait}", childNode)
        {
            this.timeToWait = timeToWait;
            this.useFixedTime = useFixedTime;
        }

        protected override void OnReset() { }

        protected override NodeStatus OnRun()
        {
            if (HasNoValidChild())
                return NodeStatus.Failure;

            float elapsedTime = Time.fixedTime - startTime;

            if (IsFirstEvalution)
            {
                StatusReason = $"Starting delay timer for {timeToWait}";
                startTime = useFixedTime ? Time.fixedTime : Time.time;
            }
            else if (elapsedTime > timeToWait)
            {
                NodeStatus childStatus = (ChildNodes[0] as Node).Run();
                StatusReason = $"Delay complete. Child node status: {childStatus}";

                return childStatus;
            }

            StatusReason = $"Delay remaining {timeToWait - elapsedTime}";
            return NodeStatus.Running;
        }
    }
}