using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public static GameMaster INSTANCE { get; private set; }

    public GameObject debugFactory;

    void Start()
    {
        if (!INSTANCE)
        {
            INSTANCE = this;
        }
        else Destroy(gameObject);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
