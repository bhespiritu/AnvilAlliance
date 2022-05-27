using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public Dictionary<int,Player> players = new Dictionary<int, Player>();

    // Start is called before the first frame update
    void Start()
    {
        //players.Add(new LocalPlayer());
        //players.Add(new NetworkPlayer());

        /*for(int i = 0; i < players.Count; i++)
        {
            players[i].id = i;
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGUI()
    {
        GUILayout.BeginArea(new Rect(0, 100, 100, 100));
        {
            //int x = int.TryParse(GUILayout.TextField("X:"));
            //int y = int.Parse(GUILayout.TextField("Y:"));
            if (GUILayout.Button("Place Building"))
            {
                ((LocalPlayer)players[0]).actionOverride = new PlaceAction();
            }
        }
        GUILayout.EndArea();
    }
}

public abstract class Player
{
    public int id;

    public abstract GameAction requestAction();
}
