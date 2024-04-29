using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public SpriteRenderer sprite;
    Color startColor;

    // Start is called before the first frame update
    void Start()
    {
        startColor = sprite.color;
    }

    // Update is called once per frame
    void Update()
    {
        if (TrenchManager.instance.CheckWithinTrench(transform.position))
        {
            sprite.color = startColor;
        }
        else
        {
            sprite.color = Color.red;
        }
    }
}
