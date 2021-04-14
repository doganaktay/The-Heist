using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public class Socialize : ActionNode
{
    public Socialize(AI owner)
    {
        this.owner = owner;
        Name = "Socialize";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Social, false);

        var timer = 0f;

        try
        {
            while (!token.IsCancellationRequested && timer < owner.CurrentSocialTime)
            {
                timer += Time.deltaTime;

                Debug.Log($"{owner.gameObject.name} is socializing");

                await UniTask.NextFrame(token);
            }
        }
        finally
        {
            if (!owner.lifetimeToken.IsCancellationRequested)
            {
                owner.IsSocializing = false;
                owner.WaitUntilCanSocialize().Forget();
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
