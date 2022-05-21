using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Networking.Transport;
using UnityEngine;

public class ActionTracker : MonoBehaviour
{
    public PlayerManager playerManager;
    public NetworkManager networkManager;

    List<ActionLogEntry> actions;

    int numFactions;

    int[] factionIndices; //useless

    ActionLogEntry currentEntry;

    public void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        playerManager = GetComponent<PlayerManager>();
        numFactions = 2;
    }

    public void collectActions()
    {
        currentEntry = new ActionLogEntry();
        currentEntry.tick = GameTime.currentTick;
        currentEntry.actions = new Action[numFactions];

        //wait for input
        for(int i = 0; i < numFactions; i++)
        {
            if(playerManager.players.ContainsKey(i))
            {
                Action a = playerManager.players[i].requestAction();
                currentEntry.actions[i] = a;
                a.PerformAction();
            }
        }

        networkManager.flushActionBuffer();

        for (int i = 0; i < numFactions; i++)
        {
            if (playerManager.players.ContainsKey(i))
            {
                Player player = playerManager.players[i];
                if (player.GetType() == typeof(LocalPlayer))
                {
                    networkManager.BroadcastAction(currentEntry.actions[i]);
                }
            }
            
        }


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
                foreach (Action action in entry.actions)
                {
                    writer.WriteLine(action.SerializeString());
                }
            }
        }
    }

    

}

public class ActionLogEntry
{
    public int tick;
    public Action[] actions;
}



public abstract class Action : IPacketData
{
    public Action(DataStreamReader stream)
    {
        Deserialize(ref stream);
    }

    public Action() {}

    public static Action getActionFromID(char ID)
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

public class NoAction : Action
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

public class PlaceAction : Action
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