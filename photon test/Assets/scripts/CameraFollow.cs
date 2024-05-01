using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow main;

    public Transform target;
    Vector3 offset;

    private void Awake()
    {
        main = this;
    }

    public void SetTarget (Transform target)
    {
        this.target = target;
        offset = transform.position - target.position;
    }

    private void LateUpdate()
    {
        if (target)
            transform.position = target.position + offset;
    }
}
