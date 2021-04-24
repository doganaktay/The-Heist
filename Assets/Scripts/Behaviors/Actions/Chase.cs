using System.Threading;
using Cysharp.Threading.Tasks;

public class Chase : ActionNode
{
    public Chase(AI owner)
    {
        this.owner = owner;
        Name = "Chase";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Chase, FOVType.Chase, true);

        var currentTargetCell = GameManager.player.CurrentCell;

        owner.SetPursuit(currentTargetCell);
        owner.Move(currentTargetCell);

        await UniTask.NextFrame(token);

        while (owner.IsMoving && !token.IsCancellationRequested)
        {
            if (currentTargetCell != GameManager.player.CurrentCell)
            {
                currentTargetCell = GameManager.player.CurrentCell;

                owner.Move(currentTargetCell);
                owner.SetPursuit(currentTargetCell);
            }

            await UniTask.NextFrame(token);
        }

        owner.IsActive = false;
        owner.ActiveActionNode = null;
    }

    protected override bool ShouldAssignAction()
    {
        if (IsCurrentAction())
        {


            return false;
        }

        return true;
    }
}
