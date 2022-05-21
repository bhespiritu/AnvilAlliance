using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public int health = 100;

    public Vector2Int position;

    public Vector2Int dimensions;

    public Faction faction;


    public event GameTime.TimeEvent OnBuildingTick;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LateStart());
    }

    IEnumerator LateStart()
    {
        yield return 0;
        GameTime.OnTick += OnBuildingTick;
    }

    private void OnDestroy()
    {
        GameTime.OnTick -= OnBuildingTick;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
