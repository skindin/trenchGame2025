using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformEffect : MonoBehaviour
{
    public Vector3 offset = Vector3.zero, scale = Vector3.one, rotation = Vector3.zero;
    public bool applied = false;

    public void ApplyEffect (bool toggle)
    {
        if (toggle != applied)
        {
            if (toggle)
            {
                transform.localPosition += offset;
                transform.localScale = Vector2.Scale(transform.localScale,scale);
                transform.eulerAngles += rotation;
            }
            else
            {
                transform.localPosition -= offset;
                transform.localScale = Vector2.Scale(transform.localScale, new Vector3(1/scale.x,1/scale.y,1/scale.z));
                transform.eulerAngles -= rotation;
            }

            applied = toggle;
        }
    }

    public void ToggleEffect()
    {
        ApplyEffect(!applied);
    }
}
