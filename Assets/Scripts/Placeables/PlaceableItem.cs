using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlaceableItemType
{
    SoundBomb
}

public class PlaceableItem : MonoBehaviour
{
    [SerializeField]
    private bool hasAOE;
    [SerializeField]
    private GameObject AOEObject;
    [SerializeField]
    private float effectRadius;
    public float EffectRadius { get => effectRadius; set => effectRadius = value; }

    private void Awake()
    {
        if (hasAOE)
            AOEObject.transform.localScale =
                new Vector3(AOEObject.transform.localScale.x * effectRadius, AOEObject.transform.localScale.y * effectRadius, AOEObject.transform.localScale.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }

    public virtual void UseItem() { }
}
