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
            if(owner.socialTargets.Count > 0)
            {
                var target = owner.socialTargets[0];
                await owner.GoTo(token, target.CurrentCell, target.transform,
                        () => { return owner.transform.IsWithinRange(target.transform, owner.transform.localScale.x * 2f) || owner.CurrentCell == target.CurrentCell; });

                while (!token.IsCancellationRequested && timer < owner.CurrentSocialTime)
                {
                    foreach (var other in owner.socialTargets)
                        if (other.CurrentBehaviorType > BehaviorType.Casual)
                        {
                            owner.CopyBehaviorState(other);
                        }

                    owner.AimOverride = true;
                    owner.Face(target.transform);

                    timer += Time.deltaTime;

                    await UniTask.NextFrame(token);
                }
            }
        }
        finally
        {
            if (!owner.lifetimeToken.IsCancellationRequested)
            {
                owner.SetFOV(FOVType.Regular);

                owner.AimOverride = false;
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
