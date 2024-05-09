using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public Character character;
    public static Controller active;

    // Start is called before the first frame update
    void Awake()
    {
        active = this; //this definitely needs improvement for multiplayer but idc rn
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 direction;

        direction.x = Input.GetAxisRaw("Horizontal");
        direction.y = Input.GetAxisRaw("Vertical");

        direction.Normalize();

        if (direction.magnitude > 0)
        {
            character.Move(direction);
        }

        if (Input.GetMouseButton(0))
        {
            //var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            character.Dig(transform.position);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            character.Dig(default, true);
        }
        else if (Input.GetMouseButton(1))
        {
            character.Fill(transform.position);
        }

        if (Input.GetMouseButtonUp(1))
        {
            character.Fill(default, true);
        }

        //if (Input.GetMouseButton(1))
        //{
        //    character.FillTrench(transform.position);
        //}
        //else
        //{
        //    character.FillTrench(Vector2.zero, true);
        //}
    }
}
