using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class LocalPlayer : Player
{
    public Action actionOverride = new NoAction();

    public override Action requestAction()
    {
        var temp = actionOverride;
        actionOverride = new NoAction();
        return temp;
    }
}
