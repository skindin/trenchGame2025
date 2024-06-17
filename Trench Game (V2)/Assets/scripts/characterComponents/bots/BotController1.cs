using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class BotController1 : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox = Vector2.one * 10;
    //public void SendData ()
    //{
    //    var chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, visionBox);

    //    var data = new DataDict<object>(4);

    //    foreach (var chunk in chunks)
    //    {
    //        if (chunk == null) continue;

    //        var itemData = new DataDict<object>(chunk.items.Count);
    //        for (var i = 0; i < chunk.items.Count; i++) //might have to also add a count key to the dictionary... shruggin emoji
    //        {
    //            var item = chunk.items[i];

    //            DataDict<object>.Combine(ref itemData, i.ToString(), item.PublicData);
    //        }
    //        DataDict< object>.Combine(ref data, Naming.items, itemData);


    //        var characterData = new DataDict<object>(chunk.characters.Count);
    //        for (var i = 0; i < chunk.characters.Count; i++) //might have to also add a count key to the dictionary... shruggin emoji
    //        {
    //            var character = chunk.items[i];

    //            DataDict<object>.Combine(ref itemData, i.ToString(), character.PublicData);
    //        }
    //        DataDict<object>.Combine(ref data, Naming.characters, characterData);

    //        var bullets = chunk.Bullets; //put this out here because it's gonna test every active bullet if it's within this chunk every time this is called
    //        var bulletData = new DataDict<object>(bullets.Count);
    //        for (int i = 0; i < bullets.Count; i++)
    //        {
    //            var bullet = bullets[i];
    //            DataDict<object>.Combine(ref bulletData, i.ToString(), bullet.Data);
    //        }

    //        DataDict<object>.Combine(ref data, Naming.bullets, bulletData);

    //        DataDict<object>.Combine(ref data, (Naming.self, character.PrivateData));

    //        //should implement bullets but whatever
    //    }
    //}

    public void Respond (string json)
    {
        var data = DataDict<object>.JsonToObj(json);

        if (data.TryKey(Naming.dir, out object moveData) && 
            moveData is DataDict<float> moveDirection &&
            moveDirection.TryKey(Naming.x, out var x) && 
            moveDirection.TryKey(Naming.y, out var y))
        {
            character.MoveInDirection(new(x,y));
        }

        //if ()
    }
}
