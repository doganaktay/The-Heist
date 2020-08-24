using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lights : MonoBehaviour
{
    List<Transform> lights = new List<Transform>();

    void Start()
    {
        for(int i=0; i < transform.childCount; i++)
        {
            lights.Add(transform.GetChild(i));
        }
    }
}
