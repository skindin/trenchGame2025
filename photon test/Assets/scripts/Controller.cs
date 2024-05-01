using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Controller : MonoBehaviourPunCallbacks
{
    public Character character;
    public static Controller main;

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
        {
            character.TestTrench(); //this line is temporary until I network trench status
            return;
        }

        CameraFollow.main.SetTarget(transform);

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
            character.Dig(Vector2.zero, true);
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
