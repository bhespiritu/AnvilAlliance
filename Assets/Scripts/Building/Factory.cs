using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Factory : MonoBehaviour
{
    Building building;

    public GameObject unitPrefab;

    public int ticksBetweenSpawn = 20*5;

    private int cooldown = 0;

    void Start()
    {
        building = GetComponent<Building>();

        building.OnBuildingTick += Tick;
    }

    void Tick()
    {
        if(cooldown == 0)
        {
            Instantiate(unitPrefab, transform.position, Quaternion.identity);

            cooldown = ticksBetweenSpawn;
        }

        cooldown--;
        if (cooldown < 0) cooldown = 0;
    }
}
