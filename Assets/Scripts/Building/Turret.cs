using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    Building building;

    Unit target;

    public float range = 5;
    public int damage = 10;

    public GameObject turretHead;

    public int ticksBetweenShots = 5;

    private int cooldown;



    // Start is called before the first frame update
    void Start()
    {
        tracerParent = GameObject.Find("Projectiles");
        building = GetComponent<Building>();

        building.OnBuildingTick += Tick;
    }

    private GameObject tracerParent;

    void Tick()
    {
        
        if (target != null)
        {
            float dist2 = (target.transform.position - transform.position).sqrMagnitude;

            if (dist2 > range*range)
            {
                target = null;
            }
            var dir = (target.transform.position - turretHead.transform.position);
            turretHead.transform.right = -dir;
            if (cooldown == 0)
            {
                var tracer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tracer.transform.localScale = Vector3.one*0.1f;
                tracer.transform.parent = tracerParent.transform;
                var rb = tracer.AddComponent<Rigidbody>();
                Destroy(tracer.GetComponent<SphereCollider>());

                rb.useGravity = false;
                rb.velocity = dir.normalized*5;
                tracer.transform.position = turretHead.transform.position;

                Destroy(tracer, 3);

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
        foreach(Unit u in FindObjectsOfType<Unit>())
        {
            float dist2 = (u.transform.position - transform.position).sqrMagnitude;

            if (dist2 < range*range)
            {
                Debug.Log("ENEMY DETECTED");
                return u;
            }
        }
        return null;
    }
    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

}
