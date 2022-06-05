using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector2 xBounds, yBounds, zBounds;

    public float panMultiplier = 1;
    public AnimationCurve panScale;
    public float zoomSpeed = 1;

    [Range(0f, 1f)]
    public float panZone = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        float percentX = mousePos.x/Screen.width;
        float percentY = mousePos.y/Screen.height;

        Vector3 currPos = transform.position;

        float panCoef = panScale.Evaluate(Mathf.InverseLerp(yBounds.x,yBounds.y,currPos.y));

        if (percentX < panZone)
        {
            currPos += Vector3.left*Time.smoothDeltaTime*panMultiplier*panCoef;
        }
        if (percentY < panZone)
        {
            currPos += Vector3.back*Time.smoothDeltaTime*panMultiplier*panCoef;
        }
        if (percentX > (1-panZone))
        {
            currPos += Vector3.right*Time.smoothDeltaTime*panMultiplier*panCoef;
        }
        if (percentY > (1-panZone))
        {
            currPos += Vector3.forward*Time.smoothDeltaTime*panMultiplier*panCoef;
        }

        currPos.y += -Input.mouseScrollDelta.y*zoomSpeed*Time.smoothDeltaTime;

        currPos.x = Mathf.Clamp(currPos.x, xBounds.x, xBounds.y);
        currPos.y = Mathf.Clamp(currPos.y, yBounds.x, yBounds.y);
        currPos.z = Mathf.Clamp(currPos.z, zBounds.x, zBounds.y);

        transform.position = currPos;
    }
}
