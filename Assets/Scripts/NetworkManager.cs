using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Networking.Transport;

public class NetworkManager : MonoBehaviour
{
    public PlayerManager playerManager;

    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;

    public NetworkConnection m_Connection;

    Dictionary<int,GameAction> actionBuffer = new Dictionary<int, GameAction>(16);

    private bool isInitialized = false;

    private bool debugClient = false;

    public Dictionary<int, int> playerConnectionIndices = new Dictionary<int, int>();

    public List<bool> hasAck { get; private set; } = new List<bool>();



    public GameAction requestAction(int player)
    {
        if(actionBuffer.ContainsKey(player))
        {
            return actionBuffer[player];
        }
        return null;
    }

    public void flushActionBuffer()
    {
        actionBuffer.Clear();
    }

    public void BroadcastAction(GameAction a)
    {
        for(int i = 0; i < m_Connections.Length; i++)
        {
            if (!hasAck[i])
            {
                Debug.Log("Broadcasting to " + i + " " + a.GetType());
                var connection = m_Connections[i];
                m_Driver.BeginSend(connection, out var writer);
                Packet p = new Packet
                {
                    packetID = 0xBA,
                    packetData = a,
                    tick = GameTime.currentTick
                };
                p.Serialize(ref writer);
                m_Driver.EndSend(writer);
            }
        }
    }

    void Start()
    {
        playerManager = GetComponent<PlayerManager>();
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    void Init()
    {
        isInitialized = true;
        if (!debugClient)
        {
            m_Driver = NetworkDriver.Create();
            var endpoint = NetworkEndPoint.AnyIpv4; // The local address to which the client will connect to is 127.0.0.1
            endpoint.Port = 9000;
            if (m_Driver.Bind(endpoint) != 0)
                Debug.Log("Failed to bind to port 9000");
            else m_Driver.Listen();

            playerManager.players[0] = new LocalPlayer { id = 0 };
        } else
        {
            m_Driver = NetworkDriver.Create();
            var endpoint = NetworkEndPoint.LoopbackIpv4; 
            endpoint.Port = 9000;
            m_Connections.Add(m_Driver.Connect(endpoint));
        }

        GameTime.OnTick += () => Tick();
    }

    void Tick()
    {
        for(int i = 0; i < hasAck.Count; i++)
        {
            hasAck[i] = false;
        }
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void Update()
    {
        if (isInitialized)
        {
            m_Driver.ScheduleUpdate().Complete();

            // CleanUpConnections
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    m_Connections.RemoveAtSwapBack(i);
                    --i;
                }
            }
            // AcceptNewConnections
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default(NetworkConnection))
            {
                m_Connections.Add(c);
                Debug.Log("Accepted a connection");
                hasAck.Add(false);

                
                playerManager.players[1] = new NetworkPlayer { id = 1, netManager = this };


                playerConnectionIndices[0] = 1;

                {
                    m_Driver.BeginSend(c, out var writer);
                    Packet registerPacket = new Packet
                    {
                        packetID = 0xAB,
                        packetData = new RegisterPacketData(0, false),
                    };
                    registerPacket.Serialize(ref writer);
                    m_Driver.EndSend(writer);
                }

                {
                    m_Driver.BeginSend(c, out var writer);
                    Packet registerPacket = new Packet
                    {
                        packetID = 0xAB,
                        packetData = new RegisterPacketData(1, true),
                    };
                    registerPacket.Serialize(ref writer);
                    m_Driver.EndSend(writer);
                }

            }

            DataStreamReader stream;
            for (int i = 0; i < m_Connections.Length; i++)
            {
                NetworkEvent.Type cmd;
                while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        short packetID = stream.ReadShort();

                        int tick = stream.ReadInt();

                        Debug.Log(stream.IsCreated + " " + packetID + " " + tick);

                        switch (packetID)
                        {
                            case 0xAB:
                                {
                                    var data = new RegisterPacketData(ref stream);

                                    int playerID = data.associatedID;
                                    if (data.fromLobby)
                                    {
                                        Debug.Log("I am now Player " + playerID);
                                        playerManager.players[playerID] = new LocalPlayer();
                                        playerManager.players[playerID].id = playerID;
                                    }
                                    else
                                    {
                                        Debug.Log(i + " is Player " + playerID);
                                        playerConnectionIndices[i] = playerID;

                                        playerManager.players[playerID] = new NetworkPlayer
                                        {
                                            id = playerID,
                                            netManager = this
                                        };
                                    }
                                }
                                break;
                            case 0xBA:
                                {
                                    if(tick == GameTime.currentTick)
                                    {
                                        char actionID = (char)stream.ReadShort();
                                        GameAction a = GameAction.getActionFromID(actionID);
                                        a.Deserialize(ref stream);

                                        actionBuffer[playerConnectionIndices[i]] = a;

                                        Debug.Log("Got Action " + a + " for Player " + playerConnectionIndices[i]);


                                        Packet ack = new Packet
                                        {
                                            tick = tick,
                                            packetID = 0x44,
                                            packetData = new EmptyPacketData()
                                        };
                                        m_Driver.BeginSend(m_Connections[i], out var writer);
                                        ack.Serialize(ref writer);
                                        m_Driver.EndSend(writer);
                                    }
                                    
                                }
                                break;
                            case 0x44:
                                {
                                    if(tick == GameTime.currentTick)
                                    {
                                        Debug.Log( i + " acknowledged for tick " + tick);
                                        hasAck[i] = true;
                                    }
                                    
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else if (cmd == NetworkEvent.Type.Connect)
                    {
                        hasAck.Add(false);
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        Debug.Log("Client disconnected from server");
                        m_Connections[i] = default(NetworkConnection);
                    }
                }
            }
        }
    }

    public bool allAcknowledged()
    {
        bool allTrue = true;
        foreach (bool b in hasAck) allTrue &= b;
        return allTrue;
    }

    void OnGUI()
    {
        if (!isInitialized)
        {
            var width = 100;
            var height = 100;
            GUILayout.BeginArea(new Rect(Screen.width - width, 0, width, height));
            if (GUILayout.Button("Host Game"))
            {
                debugClient = false;
                Init();
            }
            if (GUILayout.Button("Connect Game"))
            {
                debugClient = true;
                Init();
            }
            GUILayout.EndArea();
        }
    }


    private struct Packet
    {
        public short packetID;
        public int tick;

        public IPacketData packetData;

        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteShort(packetID);
            writer.WriteInt(tick);
            packetData.Serialize(ref writer);
        }

      
    }

}

public interface IPacketData
{
    public abstract void Serialize(ref DataStreamWriter writer);
    public abstract void Deserialize(ref DataStreamReader reader);
}

public struct EmptyPacketData : IPacketData
{
    public void Deserialize(ref DataStreamReader reader)
    {
        
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        
    }
}

public struct RegisterPacketData : IPacketData
{
    public int associatedID;
    public bool fromLobby;

    public RegisterPacketData(int val, bool lobbyFlag = false)
    {
        associatedID = val;
        fromLobby = lobbyFlag;
    }

    public RegisterPacketData(ref DataStreamReader reader)
    {
        associatedID = 0;
        fromLobby = false;
        Deserialize(ref reader);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        byte b = reader.ReadByte();
        Debug.Log("fromLobby: " + b + " " + (b == 0xFF));
        fromLobby = (b == 0xFF);
        int i = reader.ReadInt();
        Debug.Log("associatedID: " + i);
        associatedID = i;
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)(fromLobby ? 0xFF : 0));
        writer.WriteInt(associatedID);
    }
}