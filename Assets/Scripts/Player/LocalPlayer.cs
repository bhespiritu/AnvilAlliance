using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class LocalPlayer : Player
{
    public GameAction actionOverride = new NoAction();

    public override GameAction requestAction()
    {
        var temp = actionOverride;
        actionOverride = new NoAction();
        return temp;
    }
}
