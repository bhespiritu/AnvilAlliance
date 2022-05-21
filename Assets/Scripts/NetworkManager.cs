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

    Dictionary<int,Action> actionBuffer = new Dictionary<int, Action>(16);

    private bool isInitialized = false;

    private bool debugClient = false;

    public Dictionary<int, int> playerConnectionIndices = new Dictionary<int, int>();



    public Action requestAction(int player)
    {
        if(actionBuffer.ContainsKey(player))
        {
            return actionBuffer[player];
        }
        return new NoAction();
    }

    public void flushActionBuffer()
    {
        actionBuffer.Clear();
    }

    public void BroadcastAction(Action a)
    {
        foreach(NetworkConnection connection in m_Connections)
        {
            m_Driver.BeginSend(connection, out var writer);
            Packet p = new Packet
            {
                packetID = 0xBA,
                packetData = a
            };
            p.Serialize(ref writer);
            m_Driver.EndSend(writer);
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

            playerManager.players[0] = new LocalPlayer();
        } else
        {
            m_Driver = NetworkDriver.Create();
            var endpoint = NetworkEndPoint.LoopbackIpv4; 
            endpoint.Port = 9000;
            m_Connections.Add(m_Driver.Connect(endpoint));
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
                        Debug.Log(stream.IsCreated);
                        short s = stream.ReadShort();
                        Debug.Log(s);
                        short packetID = s;
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
                                    char actionID = (char)stream.ReadShort();
                                    Action a = Action.getActionFromID(actionID);
                                    a.Deserialize(ref stream);
                                    actionBuffer[playerConnectionIndices[i]] = a;

                                    Debug.Log("Got Action " + a + " for Player " + playerConnectionIndices[i]);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else if (cmd == NetworkEvent.Type.Connect)
                    {

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

        public IPacketData packetData;

        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteShort(packetID);
            packetData.Serialize(ref writer);
        }

      
    }

}

public interface IPacketData
{
    public abstract void Serialize(ref DataStreamWriter writer);
    public abstract void Deserialize(ref DataStreamReader reader);
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