using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Color dangerColor = Color.white;
    Color startColor;
    public float moveSpeed = 5, digMoveSpeed = 1, initialDigSpeed = 5;
    public TrenchAgent agent;
    public bool digging = false, filling = false;

    // Start is called before the first frame update
    void Awake()
    {
        startColor = sprite.color;
    }

    private void Start()
    {
        UpdateTrench();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Move(Vector2 direction)
    {
        Vector3 dir = direction;
        var speed = moveSpeed;
        if (digging || filling) speed = digMoveSpeed;
        transform.position += dir * speed * Time.deltaTime;

        UpdateTrench();
    }

    public void UpdateTrench()
    {
        if (agent.UpdateStatus())
        {
            sprite.color = startColor;
        }
        else
        {
            sprite.color = dangerColor;
        }
    }

    public void Dig (Vector2 digPoint, bool stop = false)
    {
        if (!stop)
        {
            agent.Dig(digPoint);
        }
        else
        {
            agent.StopDig();
        }

        digging = !stop;

        UpdateTrench();
    }

    public void FillTrench (Vector2 fillPoint, bool stop = false)
    {
        if (!stop)
            agent.FillTrench(fillPoint);

        filling = !stop;
    }
}
