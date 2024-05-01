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
    public static bool updatingTrench = false;
    //I added this property without utilizing it. It is for telling other characters to find trenches

    // Start is called before the first frame update
    void Awake()
    {
        startColor = sprite.color;
    }

    private void Start()
    {
        TestTrench();
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

        if (!digging)
            TestTrench();
    }

    public void TestTrench ()
    {
        UpdateSprite(agent.UpdateStatus());
    }

    public void UpdateSprite(bool status)
    {
        if (status)
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

        agent.SetStatus(true);
        UpdateSprite(true);
    }

    public void FillTrench (Vector2 fillPoint, bool stop = false)
    {
        if (!stop)
            agent.FillTrench(fillPoint);

        filling = !stop;
    }
}
