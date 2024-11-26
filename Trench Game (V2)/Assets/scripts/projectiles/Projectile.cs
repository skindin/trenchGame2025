using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Projectile
{
    //public int id; //not implemented
    public Vector2 pos, 
        //lastPos, 
        startPos, velocity;
    public float range;
    public Character source;
    public bool
        withinTrench = false,
        hit = false,
        destroy = false,
        startedWithinTrench = false;
    public float damage = 1;

    public int shooterLife = 0;

    //public Chunk Chunk
    //{
    //    get
    //    {
    //        return ChunkManager.Manager.ChunkFromPos(pos);
    //    }
    //}

    //public DataDict<object> Data
    //{
    //    get
    //    {
    //        var data = new DataDict<object>
    //            (
    //                "startPos", new DataDict<float>
    //                (
    //                    ("x", startPos.x),
    //                    ("y", startPos.y)
    //                )

    //            );

    //        return data;
    //    }
    //}
}
