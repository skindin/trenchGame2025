//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System;
using Google.Protobuf;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
//using System;

public class NetworkManager : ManagerBase<NetworkManager>
{
    //string id;
    public static bool IsServer{ get {
#if UNITY_EDITOR
        return Manager.editorIsServer;
#elif UNITY_SERVER
        return true;
#else
        return false;
#endif
        }}
    public bool editorIsServer;

    public GameClient client;
    public GameServer server;

    //float time;
    public static float NetTime;

    public void SetPos(Vector2 pos, int id)
    {
        var posData = DataManager.VectorToData(pos);


        if (!IsServer)
        {

            var input = new PlayerInput { Pos = posData };

            var baseMessage = new MessageForServer { Input = input };

            //var binary = DataManager.MessageToBinary(baseMessage);

            client.SendData(baseMessage.ToByteArray());

            //Debug.Log($"sent pos {pos} to server");
        }
        else {

            var charData = new CharacterData { Pos = posData, CharacterID = id };

            //server.updateCharData.List.Add(charData);

            server.UpdateCharData(charData);

            //server.SendDataDisclude(message.ToByteArray());

            //PosSyncsPerFrame++;

            //Debug.Log($"server character {id} moved to {pos}");
            }
        }

    public void SetName(string newName) //this hasn't been changed to combine with movement data but it probably should eventually
    {
        if (IsServer)
            return;

        var baseMessage = new MessageForServer() { Input = new PlayerInput { Name = newName } };

        client.SendData(baseMessage.ToByteArray());

        Debug.Log($"sent characterName {newName} to server");

        //CharacterManager.Manager.localPlayerCharacter.name = newName;

    }

    public void PickupItem (Character character, Item item)
    {
        if (!IsServer)
        {
            var input = new PlayerInput { PickupItem = item.id };

            //Item prevItem = CharacterManager.Manager.localPlayerCharacter.inventory.ActiveItem;

            var message = new MessageForServer { Input = input };

            client.SendData(message.ToByteArray());

            Debug.Log($"told server it picked up item {item.id}");// {(prevItem ? $" and dropped item {prevItem.id}" : "")}");
        }
        else
        {
            server.PickupItem(character, item);

            var characterData = new CharacterData { CharacterID = character.id, ItemId = item.id };

            server.UpdateCharData(characterData);
        }
    }

    public void RequestAmo (Ammo ammo, int amount)
    {
        if (!IsServer)
        {
            var player = CharacterManager.Manager.localPlayerCharacter;

            int index = 0;
            bool found = false;

            for (int i = 0; i < player.reserve.ammoPools.Count; i++)
            {
                var pool = player.reserve.ammoPools[i];

                if (pool.type == ammo.type)
                {
                    index = i;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"local player doesn't have amo slot for type {ammo.type.name}");
            }

            var ammoData = new AmmoData { Index = index, Amount = amount };

            var request = new AmmoRequest { Ammo = ammoData, ItemId = ammo.id };

            var input = new PlayerInput {AmmoRequest = request };

            var message = new MessageForServer { Input = input };

            client.SendData(message.ToByteArray());
        }
    }

    //public void DropItemServer(Item item, Vector2 dropPos)
    //{
    //    var posData = DataManager.VectorToData(dropPos);

    //    var itemData = new ItemData { ItemId = item.id , Pos = posData};
    //}

    public void DropItem (Item item, Vector2 pos)
    {
        if (IsServer)
        {
            //var posData = DataManager.VectorToData(pos);

            //var itemData = new ItemData { ItemId = item.id, Pos = posData };

            //server.DropItemData(itemData); 
            //it was removing items from inventory twice lol. Leaving this commented until i redesign network logic
        }
        else
        {
            var posData = DataManager.VectorToData (pos);

            var input = new PlayerInput { DropItem = posData};

            var message = new MessageForServer { Input = input };

            client.SendData(message.ToByteArray());

            Debug.Log($"told server it dropped current item");
        }

    }

    public void ServerRemoveItem (Item item)
    {
        if (!IsServer)
            return;

        server.removeItemList.Add(item.id);

        for (int i = 0; i < server.updateItems.Count; i++)
        {
            var updateItem = server.updateItems[i];

            if (updateItem.ItemId == item.id)
            {
                server.updateItems.RemoveAt(i);
                i--;
                break;
            }
        }

        for (int i = 0; i < server.currentItems.Count; i++)
        {
            var currentItem = server.currentItems[i];

            if (currentItem.ItemId == item.id)
            {
                server.currentItems.RemoveAt(i);
                Debug.Log($"removed item data {item.id}");
                i--;
                break;
            }
        }

        foreach (var currentChar in server.currentCharData.List) 
            //for some reason debuggin revealed that two characters had the same itemid very early in the game
        {
            if (currentChar.HasItemId && currentChar.ItemId == item.id)
            {
                currentChar.ClearItemId();
            }
        }

        foreach (var updateChar in server.updateCharData.List)
        {
            if (updateChar.HasItemId && updateChar.ItemId == item.id)
            {
                updateChar.ClearItemId();
            }
        }
    }

    public void SetHealth(Character character, float health)
    {
        if (!IsServer)
            return;

        var charData = new CharacterData { CharacterID = character.id, Hp = health };

        server.UpdateCharData(charData);
    }

    public void SpawnBullet(Gun gun, Bullet bullet)
    {
        var bunch = new BulletBunch { StartTime = NetTime};

        var startPosData = DataManager.VectorToData(bullet.startPos);
        var endPosData = DataManager.VectorToData(bullet.startPos + bullet.velocity.normalized * bullet.range);

        bunch.Bullets.Add(new BulletData { Startpos = startPosData, Endpos = endPosData });

        if (!IsServer)
        {
            var input = new PlayerInput { Bullets = bunch };

            var message = new MessageForServer { Input = input, Time = NetTime };

            client.SendData(message.ToByteArray());
        }
        else
        {
            bunch.CharacterId = gun.wielder.id;
            server.newBullets.Add(bunch);

            var gunData = new GunData { Amo = gun.rounds };

            var itemData = new ItemData { ItemId = gun.id, Gun = gunData };

            server.UpdateItemData(itemData);
        }

        //XPlatformLog($"told server to spawn a bullet");
    }

    public void StartReload (Gun gun)
    {
        if (!IsServer)
        {

            var input = new PlayerInput { StartReload = NetTime };

            var message = new MessageForServer { Input = input };

            client.SendData(message.ToByteArray());
        }
        else
        {
            var gunData = new GunData { ReloadStart = NetTime };
            var itemData = new ItemData { ItemId  = gun.id, Gun = gunData};

            server.UpdateItemData(itemData);
        }
    }

    public void StartConsume (MedPack consumable)
    {
        if (!IsServer)
        {
            var input = new PlayerInput { StartConsume = NetTime };

            var message = new MessageForServer { Input = input };

            client.SendData(message.ToByteArray());
        }
        else
        {
            var consumableData = new ConsumableData { ConsumeStart = NetTime };
            var itemData = new ItemData {ItemId = consumable.id, Consumable = consumableData};

            server.UpdateItemData(itemData);
        }
    }

    public Bullet DataToBullet (BulletData data, Character source, float time)
    {
        if (source.inventory.ActiveItem == null || source.inventory.ActiveItem is not Gun gun)
        {
            Debug.Log($"source {source.id} is not holding a gun");
            return null;
        }    

        var startpos = DataManager.DataToVector(data.Startpos);

        var endPos = DataManager.DataToVector(data.Endpos);

        var velocity = (endPos - startpos).normalized * gun.bulletSpeed;

        var delta = NetTime - time;

        //var progress = bullet.Progress + (delta * velocity.magnitude / gunModel.range);

        var bullet = ProjectileManager.Manager.NewBullet(startpos, velocity, gun.range, gun.DamagePerBullet, source);

        ProjectileManager.Manager.UpdateBullet(bullet,delta, out _);

        return bullet;
    }

    public void SyncDirection (Character character, Vector2 direction)
    {
        float angle = Quaternion.LookRotation(Vector3.forward, direction).eulerAngles.z;

        if (!IsServer)
        {
            var input = new PlayerInput { Angle = angle };

            var message = new MessageForServer { Input = input };

            client.SendData(message.ToByteArray());
        }
        else
        {
            var charData = new CharacterData { CharacterID = character.id, Angle = angle };

            server.UpdateCharData(charData);
        }
    }

    public void ToggleLimbo (Character character, bool toggle)
    {
        if (!IsServer)
            return;

        var data = new CharacterData { CharacterID = character.id, Limbo = toggle };

        server.UpdateCharData(data);
    }

    public void SetKills(Character character, int kills)
    {
        if (!IsServer)
            return;

        var data = new CharacterData { CharacterID = character.id, Kills = kills };

        server.UpdateCharData(data);
    }

    public void UpdateScoreboard ()
    {
        if (!IsServer)
            return;
        
        var update = new ScoreBoardUpdate { StopWatchStart = NetTime - CharacterManager.Manager.scoreStopWatch};

        if (CharacterManager.Manager.serverRecord.Value > 0)
        {
            var recordPair = CharacterManager.Manager.serverRecord;

            var serverRecord = new Record { Name = recordPair.Key, Time = recordPair.Value };

            update.ServerRecord = serverRecord;
        }

        //var index = 0;
        foreach (var character in CharacterManager.Manager.active)
        {
            update.Order.Add(character.id);
            //index++;
        }

        server.scoreboardUpdate = server.currentScoreboard = update;
    }
}
