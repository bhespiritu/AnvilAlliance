using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Networking.Transport;
using UnityEngine;

using UnityEditor;
using System;

public class ActionTracker : MonoBehaviour
{
    public PlayerManager playerManager;
    public NetworkManager networkManager;

    
    List<ActionLogEntry> actions = new List<ActionLogEntry>();

    int numFactions;

    int[] factionIndices; //useless


    [SerializeField]
    ActionLogEntry currentEntry;

    public void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        playerManager = GetComponent<PlayerManager>();
        numFactions = 2;
    }

    public IEnumerator collectActions(Action callback = null)
    {
        currentEntry = new ActionLogEntry();
        currentEntry.tick = GameTime.currentTick;
        currentEntry.actions = new GameAction[numFactions];

        bool[] recieved = new bool[numFactions];

        networkManager.flushActionBuffer();

        bool allReceived;
        do
        {
            //wait for input
            for (int i = 0; i < numFactions; i++)
            {
                if (playerManager.players.ContainsKey(i))
                {
                    GameAction a;

                    if (!recieved[i])
                    {
                        a = playerManager.players[i].requestAction();

                        Debug.Log("Waiting for " + i);
                        if (a != null)
                        {
                            currentEntry.actions[i] = a;
                            a.PerformAction();
                            recieved[i] = true;
                            currentEntry.numActionsCollected++;
                        }
                    } 
                    if (playerManager.players[i].GetType() == typeof(LocalPlayer) && recieved[i])
                    {
                        a = currentEntry.actions[i];
                        networkManager.BroadcastAction(a);
                        
                    }
                }
            }


            allReceived = true;
            for (int i = 0; i < numFactions; i++)
            {
                allReceived &= recieved[i];
            }
            if(!allReceived) yield return null;
        } while (!allReceived);

        
        actions.Add(currentEntry);

        callback?.Invoke();
    }

    public void SaveToFile()
    {
        StreamWriter writer = new StreamWriter("bruh.txt", false);

        using (writer)
        {
            writer.WriteLine(numFactions);

            writer.Write(factionIndices[0]);
            for (int i = 1; i < numFactions; i++)
            {
                writer.Write("#");
                writer.Write(factionIndices[i]);
            }
            writer.WriteLine();

            foreach (ActionLogEntry entry in actions)
            {
                writer.WriteLine(entry.tick + "==");
                foreach (GameAction action in entry.actions)
                {
                    writer.WriteLine(action.SerializeString());
                }
            }
        }
    }

    

}

[System.Serializable]
public class ActionLogEntry
{
    public int tick;
    public GameAction[] actions;
    public int numActionsCollected;
}


public abstract class GameAction : IPacketData
{
    public GameAction(DataStreamReader stream)
    {
        Deserialize(ref stream);
    }

    public GameAction() {}

    public static GameAction getActionFromID(char ID)
    {
        switch(ID)
        {
            case '0': return new NoAction();
            case 'P': return new PlaceAction();
        }
        return null;
    }

    public abstract char getActionID ();
    public abstract void Serialize(ref DataStreamWriter stream);

    public abstract string SerializeString();

    public abstract void Deserialize(ref DataStreamReader stream);

    public abstract void PerformAction();

}

public class NoAction : GameAction
{
    public override void Deserialize(ref DataStreamReader stream)
    {
        
    }

    public override char getActionID()
    {
        return '0';
    }

    public override void PerformAction() { }
    

    public override void Serialize(ref DataStreamWriter stream)
    {
        stream.WriteShort((short)getActionID());
    }

    public override string SerializeString()
    {
        return "" + getActionID();
    }
}

public class PlaceAction : GameAction
{
    public int targetX, targetY;

    public override void Deserialize(ref DataStreamReader stream)
    {
        targetX = stream.ReadInt();
        targetY = stream.ReadInt();
    }

    public override char getActionID()
    {
        return 'P';
    }

    public override void PerformAction()
    {
        GameObject.Instantiate(GameMaster.INSTANCE.debugFactory, new Vector3(targetX, 0, targetY), Quaternion.identity);
    }

    public override void Serialize(ref DataStreamWriter stream)
    {
        stream.WriteShort((short)getActionID());
        stream.WriteInt(targetX);
        stream.WriteInt(targetY);
    }

    public override string SerializeString()
    {
        return "" + getActionID() + targetX + "/" + targetY;
    }
}