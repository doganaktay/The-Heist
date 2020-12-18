using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : Character
{
    private FieldOfView fieldOfView;
    bool wandering;
    [SerializeField]
    Transform body;
    [SerializeField]
    MinMaxData lookSpeed;
    [SerializeField]
    MinMaxData waitTime;

    protected override void Start()
    {
        base.Start();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        currentSpeed = Random.Range(speed.min, speed.max);
    }

    protected override void Update()
    {
        base.Update();

        if (!wandering)
        {
            StartCoroutine(Wander());
        }
    }

    IEnumerator Wander()
    {
        wandering = true;

        Move();

        yield return null;

        while (isMoving)
            yield return null;

        yield return LookAround();

        wandering = false;
    }

    IEnumerator LookAround()
    {
        var waitTime = Random.Range(this.waitTime.min, this.waitTime.max);
        var randomRot = Quaternion.Euler(new Vector3(0f, 0f, Random.Range(25f, 180f) * Mathf.Sign(Random.Range(-1f, 1f))));
        var currentRot = transform.rotation;
        var targetRot = randomRot * currentRot;
        var lookSpeed = Random.Range(this.lookSpeed.min, this.lookSpeed.max);
        var lookCurrent = 0f;

        while (waitTime > 0)
        {
            lookCurrent += Time.deltaTime;
            var ratio = lookCurrent / lookSpeed * (180f / randomRot.eulerAngles.z);
            var t = Mathf.Min(ratio, 1f);

            transform.rotation = Quaternion.Slerp(currentRot, targetRot, t);

            if (transform.rotation == targetRot)
            {
                currentRot = transform.rotation;
                randomRot = Quaternion.Euler(new Vector3(0f, 0f, Random.Range(25f, 180f) * Mathf.Sign(Random.Range(-1f, 1f))));
                targetRot = randomRot * currentRot;
                lookSpeed = Random.Range(this.lookSpeed.min, this.lookSpeed.max);
                lookCurrent = 0f;
            }

            waitTime -= Time.deltaTime;
            yield return null;
        }
    }
}
