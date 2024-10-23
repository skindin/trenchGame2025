using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

//using UnityEngine.Android;
using WebSocketSharp;
using WebSocketSharp.Server;


public class GameServer : MonoBehaviour//: WSServerBase //none of this is gonna work lol
{
    WebSocketServer wssv;

    public Dictionary<string, Character> playerCharacters = new();
    public Dictionary<string, ClientBehavior> clients = new();

    public Queue<string> disconnected = new();

    public Queue<Action> actionQueue = new();


    public bool logBitRate = false;
    public int averageBitRateFrames = 20, averageBitRate = 0, targetFramerate = 200;

    public int bytesThisFrame { get; set; }
    List<int> pastByteRecords = new();

    long startTick = 0;



    public CharDataList newPlayerData = new(), updateCharData = new(), currentCharData = new();
    public RepeatedField<int> removeCharList = new(), removeItemList = new();

    public RepeatedField<ItemData> newItems = new(), updateItems = new(), currentItems = new();

    public RepeatedField<BulletBunch> newBullets = new();

    public ScoreBoardUpdate scoreboardUpdate, currentScoreboard;

    private void LateUpdate()
    {
        if (!NetworkManager.IsServer)
            return;
        //bool sentSomeData = actionQueue.Count > 0;


        NetworkManager.NetTime = LogicAndMath.TicksToSeconds(DateTime.UtcNow.Ticks - startTick); //just rying this

        while (actionQueue.Count > 0)
        {
            Action action;
            lock (actionQueue)
            {
                action = actionQueue.Dequeue();
            }
            action?.Invoke();
        }


        foreach (var pair in clients)
        {
            var client = pair.Value;

            foreach (var droppedItem in client.droppedItems)
            {
                DropItemData(droppedItem);
            }

            CharacterData currentData = null;
            
            if (client.character)
                currentCharData.List.FirstOrDefault(charData => charData.CharacterID == client.character.id);

            if (currentData != null && client.droppedItems.Count > 0)
                currentData.ClearItemId(); //this and the foreach probably should be outside of the if update block...

            foreach (var updatedItem in client.updatedItems)
            {
                if (!ItemManager.Manager.active.TryGetValue(updatedItem.ItemId, out var item))
                {
                    Debug.Log($"server couldn't update an item, no item with id {updatedItem.ItemId}");
                    break;
                }
                
                UpdateItemData(updatedItem);

                DataManager.UpdateItemWithData(updatedItem);
            }

            var character = client.character;

            if (character && character.inventory.ActiveWeapon != null
    && character.inventory.ActiveWeapon is Gun gun
    && client.bullets != null && client.bullets.Bullets.Count > 0 && gun.rounds > 0 && !gun.reloading)
            {
                while (client.bullets.Bullets.Count > gun.rounds)
                {
                    client.bullets.Bullets.RemoveAt(client.bullets.Bullets.Count - 1);
                }

                foreach (var bullet in client.bullets.Bullets)
                {
                    //if (gun.rounds > 0) //shouldn't need this since I'm removing them with the while loop
                    //{
                    //client.bullets.StartTime = UnityEngine.Mathf.Clamp(client.bullets.StartTime,
                    //    NetworkManager.NetTime - 1,
                    //NetworkManager.NetTime);

                    NetworkManager.Manager.DataToBullet(bullet, character, client.bullets.StartTime);
                    gun.rounds--;
                    //}
                    //else
                    //    break;
                }

                if (client.bullets.Bullets.Count > 0)
                {
                    var gunData = new GunData { Amo = gun.rounds };

                    var itemData = new ItemData { ItemId = gun.id, Gun = gunData };

                    UpdateItemData(itemData);
                }

                //Debug.Log($"spawned {client.bullets.Bullets.Count} bullet(s)");

                newBullets.Add(client.bullets);
            }

            if (client.newPlayer != null)
            {
                newPlayerData.List.Add(client.newPlayer);

                var pos = ChunkManager.Manager.GetRandomPos();
                var id = SpawnManager.Manager.NewCharId;

                var newCharacter = SpawnManager.Manager.SpawnRemoteCharacter(pos, id);

                newCharacter.characterName = client.newPlayer.Name;

                Debug.Log($"spawned remote character {id} named {client.newPlayer.Name}");

                playerCharacters.Add(client.ID, newCharacter);
                client.character = newCharacter;
                currentCharData.List.Add(client.newPlayer);

                var posData = DataManager.VectorToData(pos);

                client.newPlayer.Pos = posData;
                client.newPlayer.CharacterID = id;
            }
            else if (client.update != null)
            {
                updateCharData.List.Add(client.update);

                //var character = playerCharacters[client.ID]; //hope this doesn't cause problems

                DataManager.CombineCharDataList(client.update, currentCharData);

                if (client.update.HasName)
                {
                    character.characterName = client.update.Name;
                }

                if (client.update.Pos != null)
                {
                    var pos = DataManager.DataToVector(client.update.Pos);
                    character.SetPos(pos, false);
                }

                if (client.update.HasItemId)
                {
                    var prevItem = character.inventory.ActiveItem;

                    //var item = ItemManager.Manager.active[client.update.ItemId];

                    if (ItemManager.Manager.active.TryGetValue(client.update.ItemId, out var item))
                    {
                        PickupItem(character, item);
                    }
                    else
                    {
                        Debug.LogError($"no item with id {client.update.ItemId}");
                    }

                    //currentData.ItemId = client.update.ItemId; //i could make combinedata do this but idc rn
                    //hopefully adding the previous item to the dropped list makes the drop pos work when switching items...
                }
                //client.droppedItems.Clear();

                //if (!character.inventory.ActiveItem)
                //{
                //    currentData.ClearItemId();
                //}
                //else
                //{
                //    currentData.ItemId = character.inventory.ActiveItem.id;

                //}

                //Debug.Log($"updated character {character.id}");
                LogLists();
            }
            else if (client.remove != null)
            {

                //var character = playerCharacters[client.ID];

                removeCharList.Add(character.id);

                character.reserve.DropEverything(character.deathDropRadius);

                SpawnManager.Manager.RemoveCharacter(character);
                for (int i = 0; i < currentCharData.List.Count; i++)
                {
                    var currentChar = currentCharData.List[i];

                    //var currentChar = removeList.List[i];

                    if (currentChar.CharacterID == client.remove.CharacterID)
                    {
                        currentCharData.List.RemoveAt(i);
                        i--;


                        ItemData activeItem = null; //i have no clue what all this was for

                        if (currentChar.HasItemId)
                        {
                            foreach (var itemData in currentItems)
                            {
                                if (itemData.ItemId == currentChar.ItemId)
                                {
                                    activeItem = itemData;
                                    break;
                                }
                            }

                            activeItem.Pos = currentChar.Pos;

                            UpdateItemData(activeItem);
                        }

                        break;
                    }
                }

                Debug.Log($"removed character {character.id}, {currentCharData.List.Count} characters left");
            }

            if (client.ammoRequest != null)
            {
                if (ItemManager.Manager.active.TryGetValue(client.ammoRequest.ItemId, out var item))
                {
                    if (item is Ammo ammo)
                    {
                        var pool = client.character.reserve.ammoPools[client.ammoRequest.Ammo.Index];
                        var requestAmmoType = pool.type;

                        if (requestAmmoType == ammo.type)
                        {
                            var reqAmount = client.ammoRequest.Ammo.Amount;

                            var amtTaken = Mathf.Min(ammo.amount, reqAmount, pool.maxRounds - pool.rounds);

                            var amtLeft = Mathf.Max(ammo.amount - amtTaken, 0);

                            ammo.amount = amtLeft;

                            if (amtLeft > 0)
                            {
                                var stackData = new StackData { Amount = amtLeft };

                                var itemData = new ItemData { ItemId = ammo.id, Stack = stackData };

                                //client.ammoRequest.Ammo.Amount = amtTaken;

                                UpdateItemData(itemData);
                            }
                            else
                            {
                                //removeItemList.Add(ammo.id);
                                //not sure why this was here and and again in the network.serverremoveitem but commented out
                                ammo.DestroyItem();
                            }

                            //if (amtTaken > 0) //nvm, should probably send this back anyways

                            pool.AddAmo(amtTaken);

                            var ammoData = new AmmoData { Index = client.ammoRequest.Ammo.Index, Amount = pool.rounds };

                            client.localData = new CharacterData() { CharacterID = client.character.id};

                            client.localData.Reserve.Add(ammoData);
                        }
                        else
                        {
                            Debug.Log($"ammo request failed: item {ammo.id} is the wrong ammo type");
                        }
                    }
                    else
                    {
                        Debug.Log($"ammo request failed: item {item.id} is not ammo");
                    }
                }
                else
                {
                    Debug.Log($"ammo request failed: there is no items with id {client.ammoRequest.ItemId}"); //just until I resovle the spam
                }
            }
        }

        //if (updateCharData.List.Count > 1)
        //    Debug.Log($"{updateCharData}");

        foreach (var pair in clients)
        {
            var client = pair.Value;

            if (client.State != WebSocketState.Open)
                continue;

            //if (client.newPlayer == null && client.update == null && client.remove == null) //bruh im such an idiot
            //    continue; //doesn't need to reset, because they're all already null

            if (client.newPlayer != null) //if this client just joined, inform them of current characters, then continue to other clients
            {
                var currentChars = (currentCharData.List.Count > 0) ? currentCharData : null;

                var currentChar = currentChars.List.FirstOrDefault(charData => charData.CharacterID == client.newPlayer.CharacterID);

                if (true || client.character != null) //sloppy af ik, not sure why i was testing if the client already had a character, it should by this point
                {
                    currentChars.List.Remove(currentChar);
                }

                //client.newPlayer.Name = null;

                //var startTime = LogicAndMath.SecondsToTicks(Time.time);

                var grant = new NewPlayerGrant { NewPlayer = client.newPlayer, CurrentChars = currentChars };

                //var log = "sent ";

                foreach (var item in currentItems) //might be better to make this it's own message, this readonly thing getting annoying
                {
                    if (item.ItemId == 0)
                    {
                        Debug.Log($"ERROR: missing id: {item}");
                        //continue;
                    }
                    else
                    {
                        grant.CurrentItems.Add(item);
                        //log += item + "\n";
                    }

                    //log += item + "\n";
                }

                var serverRecord = CharacterManager.Manager.serverRecord;

                //if (currentItems.Count > 0)
                //{
                //    Debug.Log(log);
                //}

                var message = new MessageForClient { NewPlayerGrant = grant};

                if (currentScoreboard != null)
                {
                    message.ScoreBoardUpdate = currentScoreboard;
                    //Debug.Log($"sending {currentScoreboard}");
                }

                client.SendData(message.ToByteArray());

                client.connStartTick = DateTime.UtcNow.Ticks;

                currentChars.List.Add(currentChar);

                Debug.Log($"told client to spawn their player, character {client.newPlayer.CharacterID}");

                LogLists();
                client.update = client.remove = client.newPlayer = null;
                continue;
            }
            //if this is an established player...
            if (client.finishedSetup)
            {
                var gameState = new GameState();


                if (client.update != null) //this might cause a problem when the character's hp is modified when they're also moving
                {
                    var removed = updateCharData.List.Remove(client.update);

                }

                if (client.bullets != null)
                {
                    newBullets.Remove(client.bullets);
                }


                gameState.UpdateChars = updateCharData.List.Count > 0 ? updateCharData : null;

                gameState.NewRemoteChars = newPlayerData.List.Count > 0 ? newPlayerData : null;

                foreach (var removeChar in removeCharList)
                {
                    gameState.RemoveChars.Add(removeChar);
                }

                foreach (var newItem in newItems)
                {
                    if (newItem.ItemId == 0)
                    {
                        Debug.Log("ERROR: not sure how this got here, but there's an item with id 0 in new items");
                        continue;
                    }

                    if (!client.character.inventory.ActiveItem || client.character.inventory.ActiveItem.id != newItem.ItemId)
                        gameState.NewItems.Add(newItem);
                }

                foreach (var updateItem in updateItems)
                {
                    if (!client.character.inventory.ActiveItem || client.character.inventory.ActiveItem.id != updateItem.ItemId)
                    {
                        bool found = false;

                        foreach (var dropped in client.droppedItems)
                        {
                            if (dropped.ItemId == updateItem.ItemId)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            gameState.UpdateItems.Add(updateItem); //if you couldn't find this item in dropped items, update it
                        }
                    }
                }

                foreach (var removeItem in removeItemList)
                {
                    gameState.RemoveItems.Add(removeItem);
                }

                foreach (var bunch in newBullets)
                {
                    gameState.NewBullets.Add(bunch);
                }

                if (client.localData != null)
                    gameState.UpdatePlayer = client.localData;

                //if (newBullets.Count > 0)
                //{
                //    Debug.Log($"told client {client.character.id} to spawn {newBullets.Count} bullet(s)");
                //}



                if (true
                    //||
                    //gameState.UpdateChars != null ||
                    //gameState.NewRemoteChars != null ||
                    //gameState.RemoveChars != null ||
                    //gameState.NewItems.Count > 0 ||
                    //gameState.UpdateItems.Count > 0 ||
                    //gameState.RemoveItems.Count > 0 ||
                    //scoreboardUpdate != null
                    )
                {
                    var message = new MessageForClient { GameState = gameState, Time = NetworkManager.NetTime};

                    if (scoreboardUpdate != null)
                    {
                        message.ScoreBoardUpdate = scoreboardUpdate;
                        //Debug.Log($"sending {scoreboardUpdate}");
                    }

                    client.SendData(message.ToByteArray());
                    LogLists();
                }
                //else
                //    Debug.Log("apparently all the lists are empty shruggin emoji");

                if (client.update != null) //readd this character back
                {
                    updateCharData.List.Add(client.update);
                }


                foreach (var dropItem in client.droppedItems)
                {
                    updateItems.Add(dropItem);
                }

                if (client.bullets != null)
                    newBullets.Add(client.bullets);

                client.droppedItems.Clear();
                client.updatedItems.Clear();

                client.bullets = null;

                client.update = client.remove = client.newPlayer = null;

                client.localData = null;

                client.ammoRequest = null;
            }
            //LogLists();
        }

        //if (updateCharData.List.Count > 1)
        //    Debug.Log($"{updateCharData}");

        //var message = 

        newPlayerData.List.Clear();
        updateCharData.List.Clear();
        removeCharList.Clear();

        newItems.Clear();
        updateItems.Clear();
        removeItemList.Clear();

        newBullets.Clear(); //bruh facepalm emoji

        if (clients.Count > 0 && scoreboardUpdate != null)
        {
            currentScoreboard = scoreboardUpdate;
            scoreboardUpdate = null;
        }
        //if (logBitRate)
        //{
        //    pastByteRecords.Add(bytesThisFrame);

        //    if (pastByteRecords.Count > averageBitRateFrames)
        //        pastByteRecords.RemoveAt(0);

        //    averageBitRate = Mathf.RoundToInt(LogicAndMath.GetListValueTotal(pastByteRecords.ToArray(), byteCount => byteCount) / pastByteRecords.Count / Time.deltaTime);

        //    if (bytesThisFrame > 0)
        //        Debug.Log($"average bit rate: {averageBitRate}");

        //    bytesThisFrame = 0;
        //}

        while (disconnected.Count > 0)
        {
            clients.Remove(disconnected.Dequeue());
        }

        //LogLists();

        void LogLists()
        {
            //Debug.Log($"{newPlayerList.List.Count} new players, {updateList.List.Count} character updates, {removeList.List.Count} removals, and {currentCharData.List.Count} current characters");
        }
    }

    public void UpdateCharData (CharacterData data)
    {
        DataManager.CombineCharDataList(data, currentCharData);
        //DataManager.CombineCharDataList(data, updateCharData);
        updateCharData.List.Add(data); //this is so fucking sloppy because it's adding more characters bleh
    }

    public void AddCharacter(Character character)
    {
        var pos = DataManager.VectorToData(character.transform.position);
        var data = new CharacterData { CharacterID = character.id, Pos = pos, Name = character.characterName };
        currentCharData.List.Add(data);
    }


    /// <summary>
    /// doesn't update character data
    /// </summary>
    /// <param name="character"></param>
    /// <param name="item"></param>
    public void PickupItem (Character character, Item item)
    {
        character.inventory.PickupItem(item, item.transform.position);

        //if (prevItem && prevItem != character.inventory.ActiveItem)
        //{
        //    UpdateItemData(new ItemData { ItemId = prevItem.id, Pos = client.lookPos });
        //}

        foreach (var itemData in currentItems)
        {
            if (itemData.ItemId == item.id)
            {
                itemData.Pos = null;
                break;
            }
        }
    }

    public void DropItemData(ItemData droppedItem)
    {
        UpdateItemData(droppedItem);

        if (ItemManager.Manager.active.TryGetValue(droppedItem.ItemId, out var item))
        {
            if (item.wielder)
            {
                var pos = DataManager.DataToVector(droppedItem.Pos);
                item.wielder.inventory.DropItem(item, pos); //forgot to clamp drop pos lol
                var clampedPosData = DataManager.VectorToData(item.transform.position);
                droppedItem.Pos = clampedPosData;

                foreach (var currentItem in currentItems)
                {
                    if (currentItem.ItemId == droppedItem.ItemId)
                    {
                        currentItem.Pos = droppedItem.Pos;
                    }
                }
            }
            else
            {
                Debug.Log($"drop failed: item {item.id} is not being held");
            }
        }
        else
        {
            Debug.Log($"drop failed: no item with id {droppedItem.ItemId}");
        }
    }

    public void UpdateItemData (ItemData data)
    {
        DataManager.CombineItemDataList(data,currentItems);
        DataManager.CombineItemDataList(data,updateItems);
    }

    public void AddItem(Item item)
    {
        var pos = DataManager.VectorToData(item.transform.position);
        var data = new ItemData { ItemId = item.id, PrefabId = item.prefabId, Pos = pos };

        newItems.Add(data);
        currentItems.Add(data);
    }

    public void Broadcast(byte[] message)
    {
        //foreach (var session in wssv.WebSocketServices["/Echo"].Sessions) ;

        var sessions = wssv.WebSocketServices["/ClientBehavior"].Sessions;

        sessions.Broadcast(message);

        //for (var i = 0; i < sessions.Count; i++)
        //{

        //}
    }

    public void SendDataDisclude(byte[] binary, params string[] discludeIDs)
    {
        //Debug.Log($"began send data disclude. clients: {clients} discludedIds: {discludeIDs}");
        foreach (var clientBehavior in clients)
        {
            if (!discludeIDs.Contains(clientBehavior.Key))
            {
                clientBehavior.Value.SendData(binary);
            }
        }
    }

    public void SendDataToClient(byte[] binary, string id)
    {
        if (clients.TryGetValue(id, out var client))
        {
            client.SendData(binary);
        }
    }

    private void OnDestroy()
    {
        if (wssv != null)
        {
            wssv.Stop();
            wssv = null;
        }
    }

    public void SpawnLocalPlayer ()
    {
        if (NetworkManager.IsServer && !CharacterManager.Manager.localPlayerCharacter)
        {
            var pos = ChunkManager.Manager.GetRandomPos();
            var id = SpawnManager.Manager.NewCharId;

            SpawnManager.Manager.SpawnLocalPlayer(pos,id);
        }
    }

    public class ClientBehavior : WebSocketBehavior
    {
        public static GameServer server;

        public CharacterData remove, update, newPlayer, localData;

        public AmmoRequest ammoRequest;

        public List<ItemData> droppedItems = new(), updatedItems = new(); //GOTTA MAKE THIS LIST THE ITEMS IT'S DROPPING

        public Character character;

        public BulletBunch bullets = new();

        public long connStartTick;

        public bool finishedSetup = false;

        //public ClientBehavior (GameServer server)
        //{
        //    ClientBehavior.server = server;
        //}

        protected override void OnOpen()
        {
            //Debug.Log("New clientBehavior connected.");
            server.clients.Add(ID, this);
        }

        protected override void OnMessage(MessageEventArgs e)
        {


            // Use Coroutine on the main thread
            //Debug.Log("onmessage started on server");
            server.actionQueue.Enqueue(() => UseData(e.RawData));
            //Debug.Log("started coroutine on server");

            void UseData(byte[] rawData)
            {
                if (e.IsText)
                {
                    Debug.Log("Server received message: " + e.Data);
                    //return;
                }
                else if (DataManager.IfGet<MessageForServer>(rawData, out var message))
                {
                    switch (message.TypeCase)
                    {
                        case MessageForServer.TypeOneofCase.NewPlayerRequest:
                            {
                                if (!character && newPlayer == null)
                                {
                                    //var pos = ChunkManager.Manager.GetRandomPos();

                                    var name = message.NewPlayerRequest;

                                    newPlayer = new CharacterData { Name = name };

                                    Debug.Log($"recieved new player request");
                                }
                            }
                            break;

                        case MessageForServer.TypeOneofCase.Input:
                            {
                                if (character)
                                {
                                    var charData = new CharacterData { CharacterID = character.id };

                                    if (message.Input.Pos != null)
                                    {
                                        charData.Pos = message.Input.Pos;
                                    }

                                    if (message.Input.HasName)
                                    {
                                        charData.Name = message.Input.Name;
                                        //Debug.Log($"recieved new name {message.Input.Name}");
                                    }

                                    if (message.Input.HasAngle)
                                    {
                                        charData.Angle = message.Input.Angle;
                                        //Debug.Log($"recieved angle {charData.Angle} for character {character.id}");
                                    }

                                    if (update != null)
                                    {
                                        DataManager.CombineCharData(update, charData);
                                    }
                                    else
                                        update = charData;

                                    if (message.Input.HasPickupItem)
                                    {
                                        update.ItemId = message.Input.PickupItem;
                                        Debug.Log($"character {character.id} requested to pick up item {update.ItemId}");
                                    }

                                    if (message.Input.DropItem != null)
                                    {
                                        droppedItems.Add(new ItemData { ItemId = character.inventory.ActiveItem.id, Pos = message.Input.DropItem });
                                        Debug.Log($"character {character.id} dropped item {character.inventory.ActiveItem.id}");
                                    }

                                    if (message.Input.AmmoRequest != null)
                                    {
                                        ammoRequest = message.Input.AmmoRequest;
                                    }

                                    if (message.Input.HasStartReload)
                                    {
                                        if (character.inventory.ActiveItem is Gun gun)
                                        {
                                            if (gun.rounds < gun.maxRounds)
                                            {
                                                var pool = character.reserve.GetPool(gun.amoType);

                                                if (pool != null)
                                                {
                                                    if (pool.rounds > 0)
                                                    {
                                                        var gunData = new GunData { ReloadStart = message.Input.StartReload };

                                                        var itemData = new ItemData {ItemId = gun.id, Gun = gunData };

                                                        updatedItems.Add(itemData);
                                                    }
                                                    else
                                                    {
                                                        Debug.Log($"reload failed: character {character.id} doesn't have any more amo");
                                                    }
                                                }
                                                else
                                                {
                                                    Debug.Log(
                                                        $"reload failed: character {character.id} doesn't have a reserver for {gun.amoType}" +
                                                        $"");
                                                }
                                            }
                                            else
                                            {
                                                Debug.Log($"reload failed: gun {gun.id} is full");
                                            }
                                        }
                                        else
                                        {
                                            Debug.Log($"reload failed: character {character.id} isn't holding a gun");
                                        }
                                    }

                                    if (message.Input.HasStartConsume)
                                    {
                                        if (character.inventory.ActiveItem)
                                        {
                                            if (character.inventory.ActiveItem is MedPack medpack)
                                            {
                                                if (!medpack.healing)
                                                {
                                                    var consumableData = new ConsumableData { ConsumeStart = message.Input.StartConsume };

                                                    var itemData = new ItemData { ItemId = medpack.id, Consumable = consumableData };

                                                    updatedItems.Add(itemData);
                                                }
                                                else
                                                {
                                                    Debug.Log($"consume failed: character {character.id} already started healing");
                                                }
                                            }
                                            else
                                            {
                                                Debug.Log($"consume failed: character {character.id} is not holding a medpack");
                                            }
                                        }
                                        else
                                        {
                                            Debug.Log($"consume failed: character {character.id} is not holding an item");
                                        }
                                    }

                                    //lookPos = message.Input.LookPos;

                                    //Debug.Log($"server recieved input");
                                }
                                else
                                {
                                    Debug.Log($"server doesn't have a currentChar associated with client {ID}");
                                }

                                if (message.Input.Bullets != null && message.Input.Bullets.Bullets.Count > 0)
                                {
                                    bullets = message.Input.Bullets;
                                    bullets.CharacterId = character.id;
                                }
                            }
                            break;
                    }

                    if (message.HasTick)
                    {
                        var delay = DateTime.UtcNow.Ticks - connStartTick; //delay is how long this message took roundTrip
                        var delayInSeconds = LogicAndMath.TicksToSeconds(delay);

                        var remoteTick = message.Tick - (delay); //remote tick is the tick that the client thought it was when the server sent the message
                        //var ticksSinceStart = LogicAndMath.SecondsToTicks(Time.unscaledTime);

                        var remoteStartTick = remoteTick - connStartTick + server.startTick;
                        var startTickDelta = LogicAndMath.TicksToSeconds(server.startTick - remoteStartTick);

                        var startTickMessage = new MessageForClient { StartTick = remoteStartTick };

                        SendData(startTickMessage.ToByteArray());

                        finishedSetup = true;
                    }
                }
            }
        }


        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log("Client disconnected. Reason: " + e.Reason);

            server.actionQueue.Enqueue(() => RemoveCharacter(ID));

            void RemoveCharacter(string ID)
            {
                if (character)
                {
                    remove = new CharacterData { CharacterID = character.id };

                    newPlayer = update = null;

                    server.disconnected.Enqueue(ID);
                }

                //server.clients.Remove(ID);

                //if (server.playerCharacters.TryGetValue(ID, out var character))
                //{
                //    server.playerCharacters.Remove(ID);

                //    CharacterManager.Manager.RemoveCharacter(character);
                //}

                //var removeMessage = new BaseMessage() { RemoveCharOfID = character.id };

                //server.SendDataDisclude(removeMessage.ToByteArray(), ID);

                //Debug.Log($"Removed character {character.id}");
            }
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.Log("WebSocket error: " + e.Message);
        }

        public void SendData(byte[] binary)
        {
            Send(binary);

            server.bytesThisFrame += binary.Length;

            if (server.logBitRate)
                Debug.Log($"sent {binary.Length} bytes to client");
        }
    }
}
