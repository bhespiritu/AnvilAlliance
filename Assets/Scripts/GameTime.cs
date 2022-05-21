using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTime : MonoBehaviour
{

    private static GameTime INSTANCE;

    public static int currentTick { get; private set; } = 0;

    public static float timePerTick { get; private set; } =  1/20f;

    public static float gameTime => timePerTick*currentTick;

    private static float accumTime = 0;

    public delegate void TimeEvent();

    public static event TimeEvent OnStart, OnTick;

    public bool enableDebugView = true;

    public static bool isPaused = true;

    private ActionTracker actionTracker;


    // Start is called before the first frame update
    void Start()
    {
        if (!INSTANCE)
        {
            INSTANCE = this;
        }
        else Destroy(gameObject);

        actionTracker = GetComponent<ActionTracker>();

        OnStart?.Invoke();
        OnTick += () => currentTick++;
        OnTick += () => Tick();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPaused)
        {
            accumTime += Time.deltaTime;

            while (accumTime >= timePerTick)
            {
                OnTick.Invoke();
                accumTime -= timePerTick;
            }
        }
    }

    public void Tick()
    {
        actionTracker.collectActions();
    }

    private void OnGUI()
    {
        if (enableDebugView)
        {
            GUILayout.Label("Game Time:" + gameTime);
            GUILayout.Label("Current Tick:" + currentTick);
            if (GUILayout.Button("Toggle Pause"))
            {
                isPaused = !isPaused;
            }
            
        }
    }
}
