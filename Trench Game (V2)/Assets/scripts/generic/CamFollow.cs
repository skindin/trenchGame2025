using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour
{
    public static CamFollow main;
    public Transform target;
    public Vector3 delta;

    private void Awake()
    {
        if (!main) main = this;
    }

    private void Start()
    {
        if (target) AssignTarget(target);
    }

    public void AssignTarget (Transform target)
    {
        delta = transform.position - Vector3.zero;
        this.target = target;
    }

    private void LateUpdate()
    {
        if (!main) main = this;

        if (target)
            transform.position = target.position + delta;
    }

    public void Reset()
    {
        transform.position = Vector3.zero + delta;
        target = null;
    }
}
