using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int health = 100;

    public Faction faction;

    public GameObject target;

    public float moveSpeed = 5;
    public float targetBearing = 45;

    public event GameTime.TimeEvent OnUnitTick;

    public Vector2 truePosition;

    public FlowFieldManager flowFieldManager;

    // Start is called before the first frame update
    void Start()
    {
        flowFieldManager = FindObjectOfType<FlowFieldManager>();
        truePosition = new Vector2(transform.position.x, transform.position.z);
        OnUnitTick += Tick;
        GameTime.OnTick += OnUnitTick;
        
    }

    public void OnDestroy()
    {
        GameTime.OnTick -= OnUnitTick;
    }

    public void Tick()
    {
        if (health <= 0) Destroy(gameObject);

        int pX = (int)(truePosition.x + 0.5);
        int pY = (int)(truePosition.y + 0.5);

        float fX = truePosition.x - pX;
        float fY = truePosition.y - pY;
        Debug.Log(pX + " " + pY);

        Vector2 ll = flowFieldManager.getDirection(pX, pY);
        Vector2 lr = flowFieldManager.getDirection(pX, pY+1);
        Vector2 rl = flowFieldManager.getDirection(pX+1, pY);
        Vector2 rr = flowFieldManager.getDirection(pX+1, pY+1);

        var target = biLerp(new Vector2(fX, fY), ll, lr, rl, rr);
        targetBearing = Vector2.Angle(Vector2.up,flowFieldManager.getDirection(pX,pY));

        Vector3 temp = Quaternion.Euler(0, targetBearing, 0) * Vector3.forward;
        Vector2 dir = new Vector2(temp.x, temp.z);
        dir = flowFieldManager.getDirection(pX, pY);
        Debug.Log(dir);
        Debug.DrawRay(transform.position, dir * 10, Color.white, GameTime.timePerTick);
        truePosition += dir*GameTime.timePerTick*moveSpeed;


    }

    private Vector2 biLerp(Vector2 t, Vector2 a00, Vector2 a01, Vector2 a10, Vector2 a11)
    {
        Vector2 lowerX = Vector2.Lerp(a00, a01, t.x);
        Vector2 upperX = Vector2.Lerp(a10, a11, t.x);

        return Vector2.Lerp(lowerX, upperX, t.y);
    }


    void LateUpdate()
    {
        Vector3 temp = Quaternion.Euler(0, targetBearing, 0) * Vector3.forward;
        Vector2 dir = new Vector2(temp.x, temp.z);

        float timeSinceLastTick = (Time.time - GameTime.gameTime);
        Vector2 interPos = truePosition + dir*(timeSinceLastTick/GameTime.timePerTick)*0;

        transform.position = new Vector3(interPos.x, 0.25f, interPos.y);
        transform.position = new Vector3(truePosition.x, 0.25f, truePosition.y);
    }
}
