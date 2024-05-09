using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Color dangerColor = Color.white;
    Color startColor;
    public float moveSpeed = 5, digMoveSpeed = 1, initialDigSpeed = 5;
    public TrenchDetector detector;
    public TrenchDigger digger; //eventually this will be attached to the shovel...?
    public bool digging = false, filling = false, constantDig = false;

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

        if (!digging && !filling)
            UpdateTrench();
    }

    public void UpdateTrench()
    {
        UpdateSprite(detector.DetectTrench());
    }

    public void UpdateSprite (bool trenchStatus)
    {
        if (trenchStatus)
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
            digger.DigTrench(digPoint, Time.deltaTime);
        }
        else if (!constantDig)
        {
            digger.StopDigging();
        }

        digging = !stop;

        UpdateSprite(true);
        detector.SetStatus(true);
    }

    public void Fill (Vector2 fillPoint, bool stop = false)
    {
        if (stop)
        {
            digger.StopFilling();
            filling = false;
            return;
        }

        UpdateSprite(false);
        detector.SetStatus(false);

        digger.FillTrenches(fillPoint, Time.deltaTime);
        filling = true;
    }
}
