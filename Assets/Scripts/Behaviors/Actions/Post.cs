using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public class Post : ActionNode
{
    public Post(AI owner)
    {
        this.owner = owner;
        Name = "Post";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;


        var timeLimit = owner.GetPostTime();

        var indices = owner.CurrentCell.GetGraphAreaIndices();
        var index = indices[GameManager.rngFree.Range(0, indices.Count)];
        var target = GraphFinder.Areas[index].GetVantagePoint(owner.foresight);

        await owner.GoTo(token, target);

        owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Post, false);

        var directions = owner.CurrentCell.GetPostDirections();
        var selected = directions[GameManager.rngFree.Range(0, directions.Count)];

        var finalPos = (Vector2)owner.CurrentCell.transform.position + ((Vector2)selected * GameManager.CellDiagonal * 0.15f);
        await owner.GoToLocal(token, finalPos); 

        try
        {
            owner.CurrentCell.Occupied = true;
            await owner.LookAround(token, timeLimit);
        }
        finally
        {
            if (!owner.lifetimeToken.IsCancellationRequested)
            {
                owner.CurrentCell.Occupied = false;
                owner.IsPosting = false;
            }
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
