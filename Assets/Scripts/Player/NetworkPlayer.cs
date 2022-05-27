using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkPlayer : Player
{
    public NetworkManager netManager;

    public override GameAction requestAction()
    {
        return netManager.requestAction(id);
    }
}
