using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int health = 100;

    public Faction faction;

    public GameObject target;

    public float moveSpeed = 5;
    public float targetBearing = 0;

    public event GameTime.TimeEvent OnUnitTick;

    public Vector2 truePosition;

    // Start is called before the first frame update
    void Start()
    {
        
        truePosition = new Vector2(transform.position.x, transform.position.z);
        OnUnitTick += Tick;
        GameTime.OnTick += OnUnitTick;
        
    }

    public void Tick()
    {
        Vector3 temp = Quaternion.Euler(0, targetBearing, 0) * Vector3.forward;
        Vector2 dir = new Vector2(temp.x, temp.z);
        Debug.DrawRay(transform.position, temp * 10, Color.white, GameTime.timePerTick);
        truePosition += dir*GameTime.timePerTick*moveSpeed;
    }


    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 temp = Quaternion.Euler(0, targetBearing, 0) * Vector3.forward;
        Vector2 dir = new Vector2(temp.x, temp.z);

        float timeSinceLastTick = (Time.time - GameTime.gameTime);
        Vector2 interPos = truePosition + dir*(timeSinceLastTick/GameTime.timePerTick)*0;

        transform.position = new Vector3(interPos.x, 0, interPos.y);
    }
}
