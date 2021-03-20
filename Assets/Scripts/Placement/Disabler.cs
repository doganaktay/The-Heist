using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disabler : PlaceableItem
{
    [SerializeField]
    protected LayerMask affectedLayers;
    public float disableTime = -1;

    public override void Place(MazeCell cell)
    {
        base.Place(cell);
        position.IndicateAOE(notificationColor);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if((1<<collision.gameObject.layer & affectedLayers) != 0)
        {
            var guard = collision.gameObject.GetComponent<Guard>();

            guard.SetBehavior(guard.Disable(disableTime), null);

            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        position.ClearAOE(notificationColor);
    }
}
