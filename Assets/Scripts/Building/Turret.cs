using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    Building building;

    Unit target;

    public float range = 5;
    public int damage = 10;

    public int ticksBetweenShots = 5;

    private int cooldown;



    // Start is called before the first frame update
    void Start()
    {
        building = GetComponent<Building>();

        building.OnBuildingTick += Tick;
    }

    void Tick()
    {
        
        if (target != null)
        {
            float dist2 = (target.transform.position - transform.position).sqrMagnitude;

            if (dist2 > range*range)
            {
                target = null;
            }

            if(cooldown == 0)
            {
                target.health -= damage;
                cooldown = ticksBetweenShots;
            }
        }
        else target = FindTarget();

        cooldown--;
        if (cooldown < 0) cooldown = 0;
    }
    Unit FindTarget()
    {
        return null;
    }

}
