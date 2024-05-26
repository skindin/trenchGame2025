using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public Character character;
    public static Controller active;
    public bool enableFill = false, drawEdgeDetection = false;

    // Start is called before the first frame update
    void Awake()
    {
        active = this; //this definitely needs improvement for multiplayer but idc rn
    }

    bool step = false;

    // Update is called once per frame
    void Update()
    {
        //if (Time.timeScale > 0)
            Controls();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.timeScale == 0) Time.timeScale = 1;
            else Time.timeScale = 0;
        }

        if (step)
        {
            Time.timeScale = 0;
            step = false;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Time.timeScale = 10;
            step = true;
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

    public void Controls ()
    {
        Vector2 moveDir;

        moveDir.x = Input.GetAxisRaw("Horizontal");
        moveDir.y = Input.GetAxisRaw("Vertical");

        moveDir.Normalize();

        if (moveDir.magnitude > 0)
        {
            character.Move(moveDir);
        }

        //if (Input.GetMouseButton(0))
        //{
        //    //var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //    character.Dig(transform.position);
        //}
        //else if (Input.GetMouseButtonUp(0))
        //{
        //    character.Dig(default, true);
        //}
        //else if (Input.GetMouseButton(1) && enableFill)
        //{
        //    character.Fill(transform.position);
        //}

        //if (Input.GetMouseButtonUp(1))
        //{
        //    character.Fill(default, true);
        //    Time.timeScale = 1;
        //}

        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var mouseDir = mousePos - transform.position;

        if (Input.GetMouseButton(0))
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (character.gun.rounds <= 0)
                {
                    character.gun.StartReload();
                }
            }
            else
            {
                character.gun.Trigger(mouseDir);
            }
            //if (drawEdgeDetection) Trench.manager.FindTrenchEdgeFromInside(transform.position, mousePos,true);
            //chunks.Clear();
            //Chunk.manager.ChunksFromLine(transform.position, transform.position + mouseDir, chunks, true, true);
        }

        character.gun.Aim(mouseDir);
    }

    //List<Chunk> chunks = new();
}
