using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lights : MonoBehaviour
{
    List<Transform> lights = new List<Transform>();

    Coroutine rotate;

    void Awake()
    {
        for(int i=0; i < transform.childCount; i++)
        {
            lights.Add(transform.GetChild(i));
        }
    }

    public void StartRotation()
    {
        foreach(var light in lights)
        {
            light.transform.rotation = Quaternion.Euler(GameManager.rngSeeded.Range(-30f, 30f), GameManager.rngSeeded.Range(-30f, 30f), 0f);
        }

        //if(rotate != null)
        //    StopCoroutine(rotate);
        //rotate = StartCoroutine(Rotate());
    }

    IEnumerator Rotate()
    {
        while (true)
        {
            foreach (var light in lights)
            {
                light.transform.Rotate(0.5f, 0.025f, 0f);

                yield return null;

                if (Vector3.Dot(Vector3.back, -light.transform.forward) < 0f)
                {
                    light.transform.rotation *= light.transform.rotation * light.transform.rotation;
                    yield return new WaitForSeconds(1f);
                }
            }
        }
    }
}
