using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class BotController1 : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox = Vector2.one * 10;
    public DataForBot forBot;

    public void SendData ()
    {
        var chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, visionBox);

        var data = new DataDict<object>(4);

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            LogicAndMath.GetValuesList(chunk.items, forBot.items, item => DataManager.GetItemData(item), item => GeoFuncs.TestBoxPosSize(transform.position,visionBox,item.transform.position));
            LogicAndMath.GetValuesList(chunk.characters, forBot.characters, character => DataManager.GetPublicCharacterData(character), character => GeoFuncs.TestBoxPosSize(transform.position, visionBox, character.transform.position));
            //LogicAndMath.GetValuesList(chunk.Bullets, forBot.bullets, bullet => )

            //should implement bullets but whatever
        }

        forBot.self = DataManager.GetPrivateCharacterData(character);
    }

    //public void Respond (string json)
    //{
    //    var data = DataDict<object>.JsonToObj(json);

    //    if (data.TryKey(Naming.dir, out object moveData) && 
    //        moveData is DataDict<float> moveDirection &&
    //        moveDirection.TryKey(Naming.x, out var x) && 
    //        moveDirection.TryKey(Naming.y, out var y))
    //    {
    //        character.MoveInDirection(new(x,y));
    //    }

    //    //if ()
    //}

    [System.Serializable]
    public class DataForBot : JsonAble<DataForBot>
    {
        readonly public List<DataManager.PublicCharacterData> characters = new();
        readonly public List<DataManager.BaseItemData> items = new();

        //readonly public List<Bullet> bullets = new();

        public DataManager.PrivateCharacterData self;
    }
}
