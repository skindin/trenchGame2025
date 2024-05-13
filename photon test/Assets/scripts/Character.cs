using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public static List<Character> all = new();
    public SpriteRenderer sprite;
    public Color dangerColor = Color.white;
    Color startColor;
    public float moveSpeed = 5, digMoveSpeed = 1, initialDigSpeed = 5;
    public Collider collider;
    public TrenchDetector detector;
    public TrenchDigger digger; //eventually this will be attached to the shovel...?
    public Gun gun;
    public bool digging = false, filling = false, constantDig = false, constantDetect = false, shooting = false;

    // Start is called before the first frame update
    void Awake()
    {
        all.Add(this);
        startColor = sprite.color;
    }

    private void Start()
    {
        UpdateTrench();
    }

    private void OnDestroy()
    {
        all.Remove(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (constantDetect) UpdateTrench();
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
        UpdateSprite(detector.DetectTrench(0));
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

    public void Shoot (Vector2 direction)
    {
        gun.Trigger(direction);
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
