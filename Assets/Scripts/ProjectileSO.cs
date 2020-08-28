using UnityEngine;

[CreateAssetMenu(fileName = "Projectile", menuName = "Projectile", order = 1)]
public class ProjectileSO : ScriptableObject
{
    public float width;
    public float launchForceMagnitude;
    public float launchSpin;
    public int bounceLimit;
    public int[] impactLayers;
}
