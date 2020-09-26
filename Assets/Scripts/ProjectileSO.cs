using UnityEngine;

[CreateAssetMenu(fileName = "Projectile", menuName = "Projectile", order = 1)]
public class ProjectileSO : ScriptableObject
{
    public float mass;
    public float width;
    public float launchForceMagnitude;
    public float maxLaunchSpin;
    public int bounceLimit;
    public int[] impactLayers;
    public PhysMaterial physicsMaterial;
}
