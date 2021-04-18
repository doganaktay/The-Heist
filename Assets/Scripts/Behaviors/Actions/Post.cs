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
        Debug.Log($"{owner.gameObject.name} is posting");

        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

        var timeLimit = owner.GetPostTime();
        var timer = 0f;

        var directions = owner.CurrentCell.GetPostDirections();
        var selected = directions[Random.Range(0, directions.Count)];
        var invertSelected = -selected;
        var angle = Mathf.Atan2(invertSelected.y, invertSelected.x) * Mathf.Rad2Deg - 90f;
        var rot = Quaternion.AngleAxis(angle, Vector3.forward);

        var finalPos = (Vector2)owner.CurrentCell.transform.position + ((Vector2)selected * GameManager.CellDiagonal * 0.15f);
        await owner.GoToLocal(token, finalPos); 

        try
        {

            while (!token.IsCancellationRequested && timer < timeLimit)
            {
                owner.Face(rot);

                timer += Time.deltaTime;
                //await UniTask.NextFrame(token);
                await owner.LookAround(token, timeLimit);
            }
            
        }
        finally
        {
            if (!owner.lifetimeToken.IsCancellationRequested)
            {
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
