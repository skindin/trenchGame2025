using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Projectile
{
    //public int id; //not implemented
    public Vector2 pos, startPos, velocity;
    public float range;
    public Character source;
    public bool
        //withinTrench, 
        destroy = false;
    public float damage = 1;

    //public Chunk Chunk
    //{
    //    get
    //    {
    //        return ChunkManager.Manager.ChunkFromPos(pos);
    //    }
    //}

    public DataDict<object> Data
    {
        get
        {
            var data = new DataDict<object>
                (
                    "startPos", new DataDict<float>
                    (
                        ("x", startPos.x),
                        ("y", startPos.y)
                    )

                );

            return data;
        }
    }
}
