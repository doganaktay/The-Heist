using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSelector : MonoBehaviour
{
    [SerializeField]
    ProjectileSO[] projectiles;
    public ProjectileSO currentProjectile;
    public bool selectionChanged = false;

    void Start()
    {
        currentProjectile = projectiles[0];
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        { currentProjectile = projectiles[0]; selectionChanged = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        { currentProjectile = projectiles[1]; selectionChanged = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        { currentProjectile = projectiles[2]; selectionChanged = true; }
    }
}
